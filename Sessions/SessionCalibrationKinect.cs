using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.Tools;
using System.Threading;

namespace Sessions
{
    class SessionCalibrationKinect
    {
        private KinectSensor _sensor = null;
        private MultiSourceFrameReader _reader = null;
        private Body[] bodies = null;
        private int frameCount = 0;

        // Fields for calibration process
        private int _maxFramesCalibration = 100;
        private JointType _calibrationJoint = JointType.SpineBase;
        private Vector3 _calibrationResult;
        private Vector3 _estimated;
        private List<Vector3> _calibrationData;

        private List<double> _rightThighLength;
        private List<double> _rightShankLength;
        private List<double> _leftThighLength;
        private List<double> _leftShankLength;

        private CalibrationViewModel _calling;
        private TimeSpan _initialTime;

        private TimeSpan _relativeTime;

        private bool finished;

        private delegate void OneArgDelegate(string filename);

        /// <summary>
        /// Constructor for playing a clip in order to calibrate parameters
        /// </summary>
        /// <param name="callingProcess">Reference to a calling object</param>
        /// <param name="filename">Name of the clip to be played</param>
        /// <param name="numFrame">Number of frames to be evaluated</param>
        /// <param name="joint">Body joint identifier</param>
        /// <param name="initialTime">Initial time of the clip</param>
        /// <param name="estimated">Estimated joint initial position</param>
        public SessionCalibrationKinect(CalibrationViewModel callingProcess, string filename, int numFrame, 
            JointType joint, Int64 initialTime, Vector3 estimated)
        {
            _calling = callingProcess;
            _calibrationData = new List<Vector3>();
            _calibrationResult = new Vector3(0f,0f,0f);
            _maxFramesCalibration = numFrame;
            _calibrationJoint = joint;
            _initialTime = new TimeSpan(initialTime * 10000);
            _estimated = estimated;

            _relativeTime = new TimeSpan();
            _rightThighLength = new List<double>();
            _rightShankLength = new List<double>();
            _leftThighLength = new List<double>();
            _leftShankLength = new List<double>();

            finished = false;
            frameCount = 1;

            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();
                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += CalibrationFrameArrived;
            }

            if(!string.IsNullOrEmpty(filename))
            {
                _calling.CalibrationStatus = "Starting playback...";
                OneArgDelegate playback = new OneArgDelegate(PlaybackClip);
                playback.BeginInvoke(filename, null, null);
            }
        }

        /// <summary>
        /// Playback a clip
        /// </summary>
        /// <param name="filename">Path/name of the clip to be played.</param>
        private void PlaybackClip(string filename)
        {
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();

                KStudioEventStreamSelectorCollection streamCollection = new KStudioEventStreamSelectorCollection();

                using (KStudioPlayback playback = client.CreatePlayback(filename))
                {
                    // If the clip should not be evaluated from the begining.
                    // It should start paused.
                    if (_initialTime.Milliseconds > 0)
                    {
                        playback.StartPaused();
                        playback.SeekByRelativeTime(_initialTime);
                        playback.Resume();
                        
                    } else
                    {
                        playback.Start();
                    }

                    while (playback.State == KStudioPlaybackState.Playing && !finished)
                    {
                        Thread.Sleep(150);
                    }

                    // Finished the read of frames for calibration.
                    if (finished)
                    {
                        playback.Stop();
                    }
                }

                client.DisconnectFromService();
            }
        }

        /// <summary>
        /// Returns calibration result in a Vector3 (x,y,z).
        /// </summary>
        public Vector3 CalibrationResult
        {
            get { return _calibrationResult; }
        }

        /// <summary>
        /// Calculate average of X, Y, Z of registered calibration frames.
        /// </summary>
        private void CalculateCalibrationResult()
        {
            Vector3 calibrationSD = new Vector3(0f, 0f, 0f);
            Vector3 sumOfSquaresDiffs = new Vector3(0f, 0f, 0f);

            _calibrationResult.X = (float)Math.Round(_calibrationData.Average(item => item.X), 4);
            _calibrationResult.Y = (float)Math.Round(_calibrationData.Average(item => item.Y), 4);
            _calibrationResult.Z = (float)Math.Round(_calibrationData.Average(item => item.Z), 4);

            // Calculated the standard deviation of joint average position.
            // It works as a parameter to setup the threshold to accept the body joints information.
            sumOfSquaresDiffs.X = _calibrationData.Select(v => ((v.X - _calibrationResult.X) * (v.X - _calibrationResult.X))).Sum();
            sumOfSquaresDiffs.Y = _calibrationData.Select(v => ((v.Y - _calibrationResult.Y) * (v.Y - _calibrationResult.Y))).Sum();
            sumOfSquaresDiffs.Z = _calibrationData.Select(v => ((v.Z - _calibrationResult.Z) * (v.Z - _calibrationResult.Z))).Sum();

            calibrationSD.X = (float)Math.Round(Math.Sqrt(sumOfSquaresDiffs.X / _calibrationData.Count()), 3);
            calibrationSD.Y = (float)Math.Round(Math.Sqrt(sumOfSquaresDiffs.Y / _calibrationData.Count()), 3);
            calibrationSD.Z = (float)Math.Round(Math.Sqrt(sumOfSquaresDiffs.Z / _calibrationData.Count()), 3);

            _calling.CalibrationResult = _calibrationResult;
            _calling.CalibrationSD = calibrationSD;

            // Calculate observed tight and shank lengths
            _calling.RightShankLength = _rightShankLength.Average();
            _calling.RightThighLength = _rightThighLength.Average();
            _calling.LeftThighLength = _leftThighLength.Average();
            _calling.LeftShankLength = _leftShankLength.Average();

            _calling.CalibrationStatus = "Calibration done.";
        }

        /// <summary>
        /// Capture frames for calibration process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalibrationFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            _relativeTime = reference.BodyFrameReference.RelativeTime;

            using (var bodyFrame = reference.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null && !finished)
                {
                    bodies = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(bodies);
                
                    foreach (Body body in bodies)
                    {
                        if (body.IsTracked)
                        {
                            if (body.Joints[_calibrationJoint].TrackingState == TrackingState.Tracked
                               && (_estimated.X == 0 ||
                               body.Joints[_calibrationJoint].Position.X >= _estimated.X - 0.1 &&
                               body.Joints[_calibrationJoint].Position.X <= _estimated.X + 0.1) 
                               && (_estimated.Y == 0 ||
                               body.Joints[_calibrationJoint].Position.Z >= _estimated.Z - 0.1 &&
                               body.Joints[_calibrationJoint].Position.Z <= _estimated.Z + 0.1) 
                               && (_estimated.Z == 0 ||
                               body.Joints[_calibrationJoint].Position.Z >= _estimated.Z - 0.1 &&
                               body.Joints[_calibrationJoint].Position.Z <= _estimated.Z + 0.1))
                            {
                                frameCount++;

                                Console.WriteLine("Spine Z:" + body.Joints[_calibrationJoint].Position.Z);
                                CheckLimbLengths(body);

                                if (frameCount < _maxFramesCalibration)
                                {
                                    var position = body.Joints[_calibrationJoint].Position;
                                    _calibrationData.Add(new Vector3(position.X, position.Y, position.Z));
                                    _calling.ProcessedFrames = frameCount;
                                    break;
                                }
                                else
                                {
                                    _calling.ProcessedFrames = frameCount;
                                    CalculateCalibrationResult();
                                    finished = true;
                                }
                            }
                            else
                            {
                                Console.WriteLine(_relativeTime.Milliseconds + "ms NOT tracked " + frameCount);
                            }
                        }
                    }
                } else if(finished)
                {
                    frameCount = 1;
                }
            }
        }   // End of MultiFrameSourceArrived

        private void CheckLimbLengths(Body body)
        {
            // Gets right limb segment lengths
            if(body.Joints[JointType.HipRight].TrackingState == TrackingState.Tracked &&
               body.Joints[JointType.KneeRight].TrackingState == TrackingState.Tracked &&
               body.Joints[JointType.AnkleRight].TrackingState == TrackingState.Tracked)
            {
                var s = body.Joints[JointType.HipRight].Position;
                var d = body.Joints[JointType.KneeRight].Position;

                // The distance Z given by Kinect represents the distance of the object to the plane of the sensor,
                // not the center of the len. That is why we calculate the distance of the center of the len and its difference
                // to be in the same scale of the other two axis (X and Y).
                double thighLength = Math.Sqrt(Math.Pow(d.X - s.X, 2) + Math.Pow(d.Y -s.Y, 2) + Math.Pow(Util.Length(d) - Util.Length(s), 2));

                _rightThighLength.Add(thighLength);

                Console.WriteLine(_relativeTime.Milliseconds + " ms: Right Thigh " + Math.Round(thighLength, 2) + " Hip Z " + Math.Round(s.Z, 2) + 
                    " Knee Z " + Math.Round(d.Z, 2) + " Knee Y " + Math.Round(d.Y, 2));

                s = body.Joints[JointType.AnkleRight].Position;
                double shankLength = Math.Sqrt(Math.Pow(d.X - s.X, 2) + Math.Pow(d.Y - s.Y, 2) + Math.Pow(Util.Length(d) - Util.Length(s), 2));

                _rightShankLength.Add(shankLength);
            } else
            {
                Console.WriteLine(_relativeTime.Milliseconds + "ms Missed Right Limb");
            }

            // Gets left limb segment lengths
            if (body.Joints[JointType.HipLeft].TrackingState == TrackingState.Tracked &&
                body.Joints[JointType.KneeLeft].TrackingState == TrackingState.Tracked &&
                body.Joints[JointType.AnkleLeft].TrackingState == TrackingState.Tracked)
            {
                var s = body.Joints[JointType.HipLeft].Position;
                var d = body.Joints[JointType.KneeLeft].Position;

                double thighLength = Math.Sqrt(Math.Pow(d.X - s.X, 2) + Math.Pow(d.Y - s.Y, 2) + Math.Pow(Util.Length(d) - Util.Length(s), 2));

                // Console.WriteLine("Left Thight " + Math.Round(thighLength, 3));

                _leftThighLength.Add(thighLength);

                s = body.Joints[JointType.AnkleLeft].Position;
                double shankLength = Math.Sqrt(Math.Pow(d.X - s.X, 2) + Math.Pow(d.Y - s.Y, 2) + Math.Pow(Util.Length(d) - Util.Length(s), 2));

                _leftShankLength.Add(shankLength);
            }
        }
    }
}
