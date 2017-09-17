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
    class SessionKinect
    {
        private KinectSensor _sensor = null;
        private MultiSourceFrameReader _reader = null;
        private Body[] bodies = null;
        private int frameCount = 0;

        // Fields for calibration process
        private int _maxFramesCalibration = 100;
        private JointType _calibrationJoint = JointType.SpineBase;
        private Vector3 _calibrationResult;
        private List<Vector3> _calibrationData;
        private CalibrationViewModel _calling;
        private bool finished;

        private delegate void OneArgDelegate(string filename);

        /// <summary>
        /// Constructor for playing filename using KStudio.
        /// </summary>
        /// <param name="filename"></param>
        public SessionKinect(CalibrationViewModel callingProcess, string filename, int numFrame, JointType joint)
        {
            _sensor = KinectSensor.GetDefault();
            _calling = callingProcess;

            _calibrationData = new List<Vector3>();
            _calibrationResult = new Vector3(0f,0f,0f);
            _maxFramesCalibration = numFrame;
            _calibrationJoint = joint;
            finished = false;
            frameCount = 1;

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
                    playback.Start();

                    while (playback.State == KStudioPlaybackState.Playing && !finished)
                    {
                        Thread.Sleep(100);
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
            _calibrationResult.X = _calibrationData.Average(item => item.X);
            _calibrationResult.Y = _calibrationData.Average(item => item.Y);
            _calibrationResult.Z = _calibrationData.Average(item => item.Z);

            _calling.CalibrationResult = _calibrationResult;
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
                            if(body.Joints[_calibrationJoint].TrackingState == TrackingState.Tracked)
                            {
                                frameCount++;
                                if(frameCount < _maxFramesCalibration)
                                {
                                    var position = body.Joints[_calibrationJoint].Position;
                                    _calibrationData.Add(new Vector3(position.X, position.Y, position.Z));
                                    _calling.ProcessedFrames = frameCount;
                                }
                                else
                                {
                                    _calling.ProcessedFrames = frameCount;
                                    CalculateCalibrationResult();
                                    finished = true;
                                }
                            }
                        }
                    }
                } else if(finished)
                {
                    frameCount = 1;
                }
            }

        }   // End of MultiFrameSourceArrived
    }
}
