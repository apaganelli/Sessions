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

namespace Sessions
{
    class SessionAnalysisKinect
    {
        private KinectSensor _sensor;
        private MultiSourceFrameReader _reader;
        private Body[] bodies = null;
        private int frameCount = 0;

        private delegate void OneArgDelegate(ObservableCollection<VideoModel> videos);
        private bool _isPlaying = false;
        private bool _pause = false;
        private bool _stop = false;

        private Canvas _canvas;
        private Canvas _canvasImg;
        private ObservableCollection<VideoModel> _videos;
        private CalibrationModel _calibration;
        private float _threshold = 0.2f;

        private ExecutionViewModel _callingProcess;

        CameraSpacePoint _p;
        ColorSpacePoint _cpHip = new ColorSpacePoint { X = 0.0f, Y = 0.0f };
        ColorSpacePoint _cpKnee = new ColorSpacePoint { X = 0.0f, Y = 0.0f };
        ColorSpacePoint _cpAnkle = new ColorSpacePoint { X = 0.0f, Y = 0.0f };

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
            Canvas canvasImg, Canvas canvas, ExecutionViewModel calling)
        {
            _canvas = canvas;
            _canvasImg = canvasImg;
            _videos = videos;
            _calibration = calibration;

            if(_calibration == null)
            {
                _calibration = new CalibrationModel()
                {
                    CalSessionId = 0,
                    JointType = JointType.SpineBase,
                    Position = new Vector3(0f, 0f, 0f)
                };
            }

            _callingProcess = calling;

            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();
                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);
                _reader.MultiSourceFrameArrived += FrameArrived;
            }

            PlayVideoList(videos);

        }

        public void PlayVideoList(ObservableCollection<VideoModel> videos)
        {
            OneArgDelegate playback = new OneArgDelegate(PlaybackClip);
            _isPlaying = true;
            playback.BeginInvoke(videos, null, null);
        }

        public bool Pause
        {
            get { return _pause; }
            set
            {
                if(value != _pause)
                {
                    _pause = value;
                }
            }
        }

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
                        _callingProcess.ExecutionStatus = "Starting playback of " + video.Filename;

                        using (KStudioPlayback playback = client.CreatePlayback(video.Filename))
                        {
                            _isPlaying = true;
                            _stop = false;
                            _pause = false;

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

                _callingProcess.ExecutionStatus = "Finished";
            }
        }

        private void FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            // Color frame, images
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    colorBitmap = ToBitmap(frame);
                    _canvasImg.Background = new ImageBrush(colorBitmap);
                }
            }

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
                        if (body.IsTracked && 
                            body.Joints[_calibration.JointType].Position.X >= (_calibration.Position.X - _threshold) &&
                            body.Joints[_calibration.JointType].Position.X <= (_calibration.Position.X + _threshold))
                        {
                            _canvas.Children.Clear();

                            if (body.Joints[JointType.HipRight].TrackingState == TrackingState.Tracked)
                            {
                                _cpHip = _sensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[JointType.HipRight].Position);
                                _p = body.Joints[JointType.HipRight].Position;
                                _callingProcess.HipRightPosition = new Vector3(_p.X, _p.Y, _p.Z);
                            }

                            if (body.Joints[JointType.KneeRight].TrackingState == TrackingState.Tracked)
                            {
                                _cpKnee = _sensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[JointType.KneeRight].Position);
                                _p = body.Joints[JointType.KneeRight].Position;
                                _callingProcess.KneeRightPosition = new Vector3(_p.X, _p.Y, _p.Z);
                            }

                            if (body.Joints[JointType.AnkleRight].TrackingState == TrackingState.Tracked)
                            {
                                _cpAnkle = _sensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[JointType.AnkleRight].Position);
                                _p = body.Joints[JointType.AnkleRight].Position;
                                _callingProcess.AnkleRightPosition = new Vector3(_p.X, _p.Y, _p.Z);
                            }

                            DrawLimb(_cpHip, _cpKnee, _cpAnkle);

                            if (body.Joints[JointType.HipLeft].TrackingState == TrackingState.Tracked)
                            {
                                _cpHip = _sensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[JointType.HipLeft].Position);
                                _p = body.Joints[JointType.HipLeft].Position;
                                _callingProcess.HipLeftPosition = new Vector3(_p.X, _p.Y, _p.Z);
                            }

                            if (body.Joints[JointType.HipLeft].TrackingState == TrackingState.Tracked)
                            {
                                _cpKnee = _sensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[JointType.KneeLeft].Position);
                                _p = body.Joints[JointType.KneeLeft].Position;
                                _callingProcess.KneeLeftPosition = new Vector3(_p.X, _p.Y, _p.Z);
                            }

                            if (body.Joints[JointType.AnkleLeft].TrackingState == TrackingState.Tracked)
                            {
                                _cpAnkle = _sensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[JointType.AnkleLeft].Position);
                                _p = body.Joints[JointType.AnkleLeft].Position;
                                _callingProcess.AnkleLeftPosition = new Vector3(_p.X, _p.Y, _p.Z);  
                            }

                            DrawLimb(_cpHip, _cpKnee, _cpAnkle);
                        }
                    }
                }
            }
        }

        private void DrawLimb(ColorSpacePoint hip, ColorSpacePoint knee, ColorSpacePoint ankle)
        {
            DrawEllipse(ref hip);
            DrawEllipse(ref knee);
            DrawEllipse(ref ankle);

            DrawLine(hip, knee);
            DrawLine(knee, ankle);
        }

        private void DrawEllipse(ref ColorSpacePoint point)
        {
            Ellipse el = new Ellipse
            {
                Width = 10,
                Height = 10,
                StrokeThickness = 2,
                Fill = Brushes.Red
            };
            
            // Transform position proportional to canvas dimension.
            point.X = (float)(point.X * _canvas.Width) / 1080;
            point.Y = (float)(point.Y * _canvas.Height) / 1920;

            // Setup ellipse position onto canvas.
            Canvas.SetLeft(el, point.X - el.Width / 2);
            Canvas.SetTop(el, point.Y - el.Height / 2);
            _canvas.Children.Add(el);
        }

        private void DrawLine(ColorSpacePoint source, ColorSpacePoint dest)
        {
            Line line = new Line
            {
                StrokeThickness = 3,
                Stroke = Brushes.Green,
                X1 = source.X,
                X2 = dest.X,
                Y1 = source.Y,
                Y2 = dest.Y
            };

            if (_canvas != null)
            {
                _canvas.Children.Add(line);
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
