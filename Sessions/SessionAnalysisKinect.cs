using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;
using Microsoft.Kinect.Tools;
using System.Windows.Controls;
using System.Threading;
using System.Collections.ObjectModel;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Documents;

namespace Sessions
{
    class SessionAnalysisKinect
    {
        // References to Kinect objects.
        private KinectSensor _sensor;
        private MultiSourceFrameReader _reader;
        private Body[] bodies = null;
        private Int64 frameCount = 0;

        // Executes the playback in background asynchronously.
        private delegate void OneArgDelegate(ObservableCollection<VideoModel> videos);
        private bool _isPlaying = false;
        private bool _stop = false;
        private string _status = string.Empty;

        // Receives the skeleton data
        private Canvas _canvas;
        private Canvas _defaultCanvas;
        private Canvas _filteredCanvas;

        // Receives the image frames (video) from RGB stream.
        private Canvas _canvasImg;

        // Calibration information
        private CalibrationModel _calibration;
        private float _Xthreshold = 0.15f;                    // Threshold from calibration data
        private float _Ythreshold = 0.1f;                    // Threshold from calibration data
        private float _Zthreshold = 0.1f;                    // Threshold from calibration data

        // Counters to inform how many frames were tracked and not tracked.
        private Int64 _notTracked = 0;
        private Int64 _tracked = 0;

        // Reference to the calling process
        private AnalysisViewModel _callingProcess;

        // Stores all tracked points
        CameraSpacePoint[] _record;
        List<CameraSpacePoint[]> _allRecords;
     
        // ARMA filter reference for leg joints
        static readonly int N = 7;
        FilterARMA[] filtersARMA;

        // HOLT double exponential filter reference
        KinectJointFilter HoltFilter;
        CameraSpacePoint[] holtJoints;
        ColorSpacePoint[] historyTrackedJoints = new ColorSpacePoint[Body.JointCount];

        // Used to convert from camera space to image space (pixels).
        ColorSpacePoint _cpHead = new ColorSpacePoint { X = 0, Y = 0 };
        ColorSpacePoint _cpShoulderLeft = new ColorSpacePoint { X = 0, Y = 0 };
        ColorSpacePoint _cpShoulderRight = new ColorSpacePoint { X = 0, Y = 0 };
        ColorSpacePoint _cpSpineShoulder = new ColorSpacePoint { X = 0, Y = 0 };
        ColorSpacePoint _cpSpineBase = new ColorSpacePoint { X = 0, Y = 0 };

        ColorSpacePoint _cpElbowLeft = new ColorSpacePoint { X = 0, Y = 0 };
        ColorSpacePoint _cpElbowRight = new ColorSpacePoint { X = 0, Y = 0 };
        ColorSpacePoint _cpWristLeft = new ColorSpacePoint { X = 0, Y = 0 };
        ColorSpacePoint _cpWristRight = new ColorSpacePoint { X = 0, Y = 0 };

        CameraSpacePoint _p;
        ColorSpacePoint _cpHipL = new ColorSpacePoint { X = 0.0f, Y = 0.0f };
        ColorSpacePoint _cpKneeL = new ColorSpacePoint { X = 0.0f, Y = 0.0f };
        ColorSpacePoint _cpAnkleL = new ColorSpacePoint { X = 0.0f, Y = 0.0f };
        ColorSpacePoint _cpHipR = new ColorSpacePoint { X = 0.0f, Y = 0.0f };
        ColorSpacePoint _cpKneeR = new ColorSpacePoint { X = 0.0f, Y = 0.0f };
        ColorSpacePoint _cpAnkleR = new ColorSpacePoint { X = 0.0f, Y = 0.0f };

        // Used to receives bitmap images from RGB sensor and send it to a canvas.
        private ImageSource colorBitmap = null;

        /// <summary>
        /// Constructor for playing back videos, show and collect data.
        /// </summary>
        /// <param name="videos">List of videos to be played</param>
        /// <param name="calibration">Calibration data</param>
        /// <param name="canvasImg">Pointer to RGB image</param>
        /// <param name="canvas">Pointer to skeleton</param>
        /// <param name="calling">Pointer to calling process</param>
        public SessionAnalysisKinect(ObservableCollection<VideoModel> videos, CalibrationModel calibration, 
            Canvas canvasImg, Canvas canvas,  Canvas filteredCanvas  ,AnalysisViewModel calling)
        {
            _canvas = canvas;
            _canvasImg = canvasImg;
            _calibration = calibration;
            _filteredCanvas = filteredCanvas;

            _tracked = 0;
            _notTracked = 0;
            frameCount = 0;

             _defaultCanvas = _canvas;

            // If the session has no calibration initialize the instance.
            if (_calibration == null)
            {
                _calibration = new CalibrationModel()
                {
                    CalSessionId = 0,
                    JointType = JointType.SpineBase,
                    Position = new Vector3(0f, 0f, 0f)
                };
            } else
            {
                _Xthreshold = _calibration.Threshold.X;
            }

            // A reference to the calling process that receives and shows data.
            _callingProcess = calling;

            InitializeFilterARMA();
            HoltFilter = new KinectJointFilter();
            HoltFilter.Init(0.5f);

            _allRecords = new List<CameraSpacePoint[]>();

            //
            // This block bellow start Kinect device.
            //
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();
                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);    
                _reader.MultiSourceFrameArrived += FrameArrived;
            }

            // Send the video list to be played.
            PlayVideoList(videos);
        }


        private void InitializeFilterARMA()
        {
            filtersARMA = new FilterARMA[Body.JointCount];

            for(int i=0; i<Body.JointCount; i++)
            {
                filtersARMA[i] = new FilterARMA(N);
            }
        }

        /// <summary>
        /// Closes kinect sensor and event reader
        /// </summary>
        public void CloseAll()
        {
            _reader.MultiSourceFrameArrived -= FrameArrived;
            _reader = null;
            _sensor.Close();
        }

        /// <summary>
        /// Receives a list of video model objects to be played asynchronously.
        /// </summary>
        /// <param name="videos">An ObservableCollection of VideoModel to be played.</param>
        public void PlayVideoList(ObservableCollection<VideoModel> videos)
        {
            OneArgDelegate playback = new OneArgDelegate(PlaybackClip);
            _isPlaying = true;
            playback.BeginInvoke(videos, null, null);
        }

 
        /// <summary>
        /// Flag to stop playing the video.
        /// </summary>
        public bool Stop
        {
            get { return _stop; }
            set
            {
                if(value != _stop)
                {
                    _stop = value;
                }
            }
        }

        /// <summary>
        /// Gets playback status, whether it is playing or not.
        /// </summary>
        public bool IsPlaying
        {
            get { return _isPlaying; }
        }

        /// <summary>
        /// Playback a list of clips
        /// </summary>
        /// <param name="filename">ObservableCollection of VideoModel objects that contains Filename of the clip to be played.</param>
        private void PlaybackClip(ObservableCollection<VideoModel> videos)
        {
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();
                KStudioEventStreamSelectorCollection streamCollection = new KStudioEventStreamSelectorCollection();
                VideoModel video;
                int i = 0;

                while (i < videos.Count)
                {
                    video = videos[i];

                    if (!string.IsNullOrEmpty(video.Filename))
                    {
                        _status = "Playing " + video.Filename + " - " + (i+1) + "/" + videos.Count;

                        using (KStudioPlayback playback = client.CreatePlayback(video.Filename))
                        {
                            _isPlaying = true;
                            _stop = false;

                            // We can use playback.StartPaused() and SeekRelativeTime to start the clip at
                            // a specific time.
                            playback.Start();
                            

                            while (playback.State == KStudioPlaybackState.Playing)
                            {
                                Thread.Sleep(150);

                                if (_stop)
                                {
                                    playback.Stop();
                                    break;
                                }
                            }

                            _isPlaying = false;

                            if (_stop)
                                return;
                        }
                    }
                    i++;
                }
                client.DisconnectFromService();
            }
        }

 
        /// <summary>
        /// Event triggered when a frame is captured.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            _callingProcess.ExecutionStatus = _status;

            // Trying to sync playback control and frame acquisition event.
            // When playback is not running, it should not update image and positions.
            if (!_isPlaying)
            {
                return;
            }

            var reference = e.FrameReference.AcquireFrame();

            // Body frame, joint positions
            using (var bodyFrame = reference.BodyFrameReference.AcquireFrame())
            {
                frameCount++;

                if (bodyFrame != null)
                {
                    bodies = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    foreach (Body body in bodies)
                    {
                        if(! body.IsTracked)
                        {
                            continue;
                        }

                        // Gets raw position information from chosen calibration joint.
                        _p = body.Joints[_calibration.JointType].Position;

                        // Check if calibration joint position is within expected individual position.
                        if (_calibration.Position.X == 0 || _p.X >= (_calibration.Position.X - _Xthreshold) && _p.X <= (_calibration.Position.X + _Xthreshold) &&
                           (_calibration.Position.Y == 0 || _p.Y >= (_calibration.Position.Y - _Ythreshold) && _p.Y <= (_calibration.Position.Y + _Ythreshold)) &&
                           (_calibration.Position.Z == 0 || _p.Z >= (_calibration.Position.Z - _Zthreshold) && _p.Z <= (_calibration.Position.Z + _Zthreshold)))
                        {
                            _tracked++;

                            // show raw data from calibration joint
                            _callingProcess.CalibrationJoint = new Vector3(_p.X, _p.Y, _p.Z);

                            _record = new CameraSpacePoint[Body.JointCount];

                            for(int i=0; i<=(int)JointType.ThumbRight; i++)
                            {
                                _record[i] = body.Joints[(JointType)i].Position;
                            }

                            _allRecords.Add(_record);

                            // Head, Shoulders, Arm and trunk
                            _cpHead = ConvertJoint2ColorSpace(body.Joints[JointType.Head], filtersARMA[(int)JointType.Head]);
                            _cpShoulderLeft = ConvertJoint2ColorSpace(body.Joints[JointType.ShoulderLeft], filtersARMA[(int)JointType.ShoulderLeft]);
                            _cpShoulderRight = ConvertJoint2ColorSpace(body.Joints[JointType.ShoulderRight], filtersARMA[(int)JointType.ShoulderRight]);
                            _cpElbowRight = ConvertJoint2ColorSpace(body.Joints[JointType.ElbowRight], filtersARMA[(int)JointType.ElbowRight]);
                            _cpWristRight = ConvertJoint2ColorSpace(body.Joints[JointType.WristRight], filtersARMA[(int)JointType.WristRight]);
                            _cpSpineShoulder = ConvertJoint2ColorSpace(body.Joints[JointType.SpineShoulder], filtersARMA[(int)JointType.SpineShoulder]);
                            _cpElbowLeft = ConvertJoint2ColorSpace(body.Joints[JointType.ElbowLeft], filtersARMA[(int)JointType.ElbowLeft]);
                            _cpWristLeft = ConvertJoint2ColorSpace(body.Joints[JointType.WristLeft], filtersARMA[(int)JointType.WristLeft]);
                            _cpSpineBase = ConvertJoint2ColorSpace(body.Joints[JointType.SpineBase], filtersARMA[(int)JointType.SpineBase]);

                            _cpHipR = ConvertJoint2ColorSpace(body.Joints[JointType.HipRight], filtersARMA[(int)JointType.HipRight]);
                            _cpKneeR = ConvertJoint2ColorSpace(body.Joints[JointType.KneeRight], filtersARMA[(int)JointType.KneeRight]);
                            _cpAnkleR = ConvertJoint2ColorSpace(body.Joints[JointType.AnkleRight], filtersARMA[(int)JointType.AnkleRight]);

                            _cpHipL = ConvertJoint2ColorSpace(body.Joints[JointType.HipLeft],   filtersARMA[(int)JointType.HipLeft]);
                            _cpKneeL = ConvertJoint2ColorSpace(body.Joints[JointType.KneeLeft], filtersARMA[(int)JointType.KneeLeft]);
                            _cpAnkleL = ConvertJoint2ColorSpace(body.Joints[JointType.AnkleLeft], filtersARMA[(int)JointType.AnkleLeft]);
                            
                            // Draw limbs on canvas.
                            _defaultCanvas = _canvas;
                            _defaultCanvas.Children.Clear();

                            DrawHeadShoulder(_cpHead, _cpSpineShoulder, _cpSpineBase, _cpShoulderLeft, _cpShoulderRight,
                                             _cpElbowLeft, _cpElbowRight, _cpWristLeft, _cpWristRight);
                            DrawLimb(_cpHipL, _cpKneeL, _cpAnkleL);
                            DrawLimb(_cpHipR, _cpKneeR, _cpAnkleR);
                            CheckLimbLengths(body);

                            // Removes jitter and apply Holt Double Exponential filter
                            _defaultCanvas = _filteredCanvas;
                            _defaultCanvas.Children.Clear();

                            HoltFilter.UpdateFilter(body);
                            holtJoints = HoltFilter.GetFilteredJoints();
                            ShowHoltJoints(holtJoints);
                            break;
                        }                        
                    } // For each body
                }
            } // end of using body frames.

            _callingProcess.NumFrames = frameCount;
            _callingProcess.NotTracked = _notTracked;
            _callingProcess.Tracked = _tracked;
            _callingProcess.Records = _allRecords;

            // Color frame, images
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    colorBitmap = ToBitmap(frame);
                    _canvasImg.Background = new ImageBrush(colorBitmap);
                }
            }
        }

        /// <summary>
        /// Converts camera space point to color space, shows position to calling process and applies ARMA filter if the case.
        /// </summary>
        /// <param name="joint">Body joint</param>
        /// <param name="filter">ARMA filter reference to this joint</param>
        /// <returns></returns>
        private ColorSpacePoint ConvertJoint2ColorSpace(Joint joint, FilterARMA filter)
        {
            ColorSpacePoint point = new ColorSpacePoint();

            if (joint.TrackingState == TrackingState.Tracked)
            {

                switch(joint.JointType)
                {
                    case JointType.HipRight:
                        _callingProcess.HipRightPosition = new Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z);
                        break;
                    case JointType.HipLeft:
                        _callingProcess.HipLeftPosition = new Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z);
                        break;
                    case JointType.KneeRight:
                        _callingProcess.KneeRightPosition = new Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z);
                        break;
                    case JointType.KneeLeft:
                        _callingProcess.KneeLeftPosition = new Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z);
                        break;
                    case JointType.AnkleRight:
                        _callingProcess.AnkleRightPosition = new Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z);
                        break;
                    case JointType.AnkleLeft:
                        _callingProcess.AnkleLeftPosition = new Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z);
                        break;
                }

                point = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
                historyTrackedJoints[(int)joint.JointType] = point;

                if (filter != null)
                {
                    filter.UpdateSerie(joint.Position);
                }
            }
            else
            {
                if (filter != null)
                {
                    joint.Position = filter.PredictNextPoint();
                    point = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
                }
                else
                {
                    point = historyTrackedJoints[(int)joint.JointType];
                }

                _notTracked++;
            }
            return point;
        }

        /// <summary>
        /// Converts Holt filtered position into camera space and draws skeleton.
        /// </summary>
        /// <param name="joints">Array of output joint positions of Holt Filter.</param>
        private void ShowHoltJoints(CameraSpacePoint[] joints)
        {
            _cpHead = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.Head]);
            _cpShoulderLeft = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.ShoulderLeft]);
            _cpShoulderRight = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.ShoulderRight]);
            _cpElbowRight = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.ElbowRight]);
            _cpWristRight = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.WristRight]);
            _cpSpineShoulder = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.SpineShoulder]);
            _cpElbowLeft = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.ElbowLeft]);
            _cpWristLeft = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.WristLeft]);
            _cpSpineBase = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.SpineBase]);
            _cpHipR = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.HipRight]);
            _cpKneeR = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.KneeRight]);
            _cpAnkleR = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.AnkleRight]);
            _cpHipL = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.HipLeft]);
            _cpKneeL = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.KneeLeft]);
            _cpAnkleL = _sensor.CoordinateMapper.MapCameraPointToColorSpace(joints[(int)JointType.AnkleLeft]);

            DrawHeadShoulder(_cpHead, _cpSpineShoulder, _cpSpineBase, _cpShoulderLeft, _cpShoulderRight,
                            _cpElbowLeft, _cpElbowRight, _cpWristLeft, _cpWristRight);
            DrawLimb(_cpHipL, _cpKneeL, _cpAnkleL);
            DrawLimb(_cpHipR, _cpKneeR, _cpAnkleR);
        }


        private void CheckLimbLengths(Body body)
        {
            // Gets right limb segment lengths
            if (body.Joints[JointType.HipRight].TrackingState == TrackingState.Tracked &&
               body.Joints[JointType.KneeRight].TrackingState == TrackingState.Tracked &&
               body.Joints[JointType.AnkleRight].TrackingState == TrackingState.Tracked)
            {
                var s = body.Joints[JointType.HipRight].Position;
                var d = body.Joints[JointType.KneeRight].Position;

                // The distance Z given by Kinect represents the distance of the object to the plane of the sensor,
                // not the center of the len. That is why we calculate the distance of the center of the len and its difference
                // to be in the same scale of the other two axis (X and Y).
                _callingProcess.RightThighLength = Math.Round(Math.Sqrt(Math.Pow(d.X - s.X, 2) + Math.Pow(d.Y - s.Y, 2) + Math.Pow(Util.Length(d) - Util.Length(s), 2)),3);

                s = body.Joints[JointType.AnkleRight].Position;
                _callingProcess.RightShankLength = Math.Round(Math.Sqrt(Math.Pow(d.X - s.X, 2) + Math.Pow(d.Y - s.Y, 2) + Math.Pow(Util.Length(d) - Util.Length(s), 2)),3);
            }

            // Gets left limb segment lengths
            if (body.Joints[JointType.HipLeft].TrackingState == TrackingState.Tracked &&
                body.Joints[JointType.KneeLeft].TrackingState == TrackingState.Tracked &&
                body.Joints[JointType.AnkleLeft].TrackingState == TrackingState.Tracked)
            {
                var s = body.Joints[JointType.HipLeft].Position;
                var d = body.Joints[JointType.KneeLeft].Position;

                _callingProcess.LeftThighLength = Math.Round(Math.Sqrt(Math.Pow(d.X - s.X, 2) + Math.Pow(d.Y - s.Y, 2) + Math.Pow(Util.Length(d) - Util.Length(s), 2)),3);

                s = body.Joints[JointType.AnkleLeft].Position;
                _callingProcess.LeftShankLength = Math.Round(Math.Sqrt(Math.Pow(d.X - s.X, 2) + Math.Pow(d.Y - s.Y, 2) + Math.Pow(Util.Length(d) - Util.Length(s), 2)),3);
            }
        }

        /// <summary>
        /// Draws head-shoulder
        /// </summary>
        /// <param name="head"></param>
        /// <param name="spine"></param>
        /// <param name="leftShoulder"></param>
        /// <param name="rightShoulder"></param>
        private void DrawHeadShoulder(ColorSpacePoint head, ColorSpacePoint spineHigh, ColorSpacePoint spineLow,  
            ColorSpacePoint leftShoulder, ColorSpacePoint rightShoulder,
            ColorSpacePoint leftElbow, ColorSpacePoint rightElbow,
            ColorSpacePoint leftWrist, ColorSpacePoint rightWrist)
        {
            DrawEllipse(ref head, 30);
            DrawEllipse(ref spineHigh, 2);
            DrawEllipse(ref spineLow, 2);
            DrawEllipse(ref leftShoulder, 5);
            DrawEllipse(ref rightShoulder, 5);
            DrawEllipse(ref leftElbow, 5);
            DrawEllipse(ref rightElbow, 5);
            DrawEllipse(ref leftWrist, 5);
            DrawEllipse(ref rightWrist, 5);
            
            DrawLine(head, spineHigh);
            DrawLine(leftShoulder, spineHigh);
            DrawLine(rightShoulder, spineHigh);
            DrawLine(spineLow, spineHigh);
            DrawLine(leftShoulder, leftElbow);
            DrawLine(leftElbow, leftWrist);
            DrawLine(rightShoulder, rightElbow);
            DrawLine(rightElbow, rightWrist);
        }


        /// <summary>
        /// Draws limbs 
        /// </summary>
        /// <param name="hip">Hip Position</param>
        /// <param name="knee">Knee Position</param>
        /// <param name="ankle">Ankle Position</param>
        private void DrawLimb(ColorSpacePoint hip, ColorSpacePoint knee, ColorSpacePoint ankle)
        {
            DrawEllipse(ref hip, 10);
            DrawEllipse(ref knee, 10);
            DrawEllipse(ref ankle, 10);

            DrawLine(hip, knee);
            DrawLine(knee, ankle);
        }

        private void DrawEllipse(ref ColorSpacePoint point, float radius)
        {
            if (float.IsInfinity(point.X) || float.IsInfinity(point.Y))
                return;

            Ellipse el = new Ellipse
            {
                Width = radius,
                Height = radius,
                StrokeThickness = 2,
                Fill = Brushes.Red
            };
            
            // Transform position proportional to canvas dimension.
            point.X = (float)(point.X * _canvas.Width) / 1920;
            point.Y = (float)(point.Y * _canvas.Height) / 1080;

            // Setup ellipse position onto canvas.
            Canvas.SetLeft(el, point.X - el.Width / 2);
            Canvas.SetTop(el, point.Y - el.Height / 2);
            _defaultCanvas.Children.Add(el);
        }

        private void DrawLine(ColorSpacePoint source, ColorSpacePoint dest)
        {
            if (float.IsInfinity(source.X) || float.IsInfinity(source.Y) ||
                float.IsInfinity(dest.X) || float.IsInfinity(dest.Y))
                return;

            Line line = new Line
            {
                StrokeThickness = 3,
                Stroke = Brushes.Green,
                X1 = source.X,
                X2 = dest.X,
                Y1 = source.Y,
                Y2 = dest.Y
            };

            if (_defaultCanvas != null)
            {
                _defaultCanvas.Children.Add(line);
            }
        }

        // Color Frame to bitmap
        private ImageSource ToBitmap(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            byte[] pixels = new byte[width * height * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
        }
    }
}
