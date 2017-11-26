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
using System.Windows;

namespace Sessions
{
    /// <summary>
    /// Author: Antonio Iyda Paganelli
    /// 
    /// </summary>
    class SessionAnalysisKinect
    {
        // References to Kinect objects.
        private MultiSourceFrameReader _reader;
        private Body[] bodies = null;
        private Int64 frameCount = 0;

        private CoordinateMapper _mapper;

        // Receives the skeleton data
        private Canvas _canvas;

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

        ColorSpacePoint[] historyTrackedJoints = new ColorSpacePoint[Body.JointCount]; 

        // ARMA filter reference for leg joints
        static readonly int N = 7;
        FilterARMA[] filtersARMA;

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

        /// <summary>
        /// Constructor for playing back videos, show and collect data.
        /// </summary>
        /// <param name="calibration">Calibration data</param>
        /// <param name="canvas">Pointer to skeleton</param>
        /// <param name="calling">Pointer to calling process</param>
        public SessionAnalysisKinect(KinectSensor sensor, CalibrationModel calibration,
             Canvas canvas, AnalysisViewModel calling)
        {
            _calibration = calibration;
            _canvas = canvas;

            _tracked = 0;
            _notTracked = 0;
            frameCount = 0;

            // If the session has no calibration initialize the instance.
            if (_calibration == null)
            {
                _calibration = new CalibrationModel()
                {
                    CalSessionId = 0,
                    JointType = JointType.SpineBase,
                    Position = new Vector3(0f, 0f, 0f)
                };
            }
            else
            {
                _Xthreshold = _calibration.Threshold.X;
                _Ythreshold = _calibration.Threshold.Y;
                _Zthreshold = _calibration.Threshold.Z;
            }

            // A reference to the calling process that receives and shows data.
            _callingProcess = calling;

            InitializeFilterARMA();

            if (sensor != null)
            {
                _mapper = sensor.CoordinateMapper;
                _reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += FrameArrived;                    
            }
        }

        /// <summary>
        /// Initialize the array of ARMA filters with the number of frames to be considered for each body joint.
        /// </summary>
        private void InitializeFilterARMA()
        {
            filtersARMA = new FilterARMA[Body.JointCount];

            for (int i = 0; i < Body.JointCount; i++)
            {
                filtersARMA[i] = new FilterARMA(N);
            }
        }

        /// <summary>
        /// Closes kinect sensor and event reader
        /// </summary>
        public void Dispose()
        {
            _reader.MultiSourceFrameArrived -= FrameArrived;
            _reader = null;
        }

        /// <summary>
        /// Event triggered when a frame is captured.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
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
                        if (!body.IsTracked)
                        {
                            continue;
                        }

                        // Gets raw position information from chosen calibration joint.
                        _p = body.Joints[_calibration.JointType].Position;

                        // Check if calibration joint position is within expected individual position.
                        if ((_calibration.Position.X == 0 || _p.X >= (_calibration.Position.X - _Xthreshold) && _p.X <= (_calibration.Position.X + _Xthreshold)) &&
                           (_calibration.Position.Y == 0 || _p.Y >= (_calibration.Position.Y - _Ythreshold) && _p.Y <= (_calibration.Position.Y + _Ythreshold)) &&
                           (_calibration.Position.Z == 0 || _p.Z >= (_calibration.Position.Z - _Zthreshold) && _p.Z <= (_calibration.Position.Z + _Zthreshold)))
                        {
                            _tracked++;
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

                            _cpHipL = ConvertJoint2ColorSpace(body.Joints[JointType.HipLeft], filtersARMA[(int)JointType.HipLeft]);
                            _cpKneeL = ConvertJoint2ColorSpace(body.Joints[JointType.KneeLeft], filtersARMA[(int)JointType.KneeLeft]);
                            _cpAnkleL = ConvertJoint2ColorSpace(body.Joints[JointType.AnkleLeft], filtersARMA[(int)JointType.AnkleLeft]);

                            // Draw limbs on canvas.
                            _canvas.Children.Clear();
                            DrawHeadShoulder(_cpHead, _cpSpineShoulder, _cpSpineBase, _cpShoulderLeft, _cpShoulderRight,
                                             _cpElbowLeft, _cpElbowRight, _cpWristLeft, _cpWristRight);
                            DrawLimb(_cpHipL, _cpKneeL, _cpAnkleL);
                            DrawLimb(_cpHipR, _cpKneeR, _cpAnkleR);
                            break;
                        }
                    } // For each body
                }
            } // end of using body frames.

            _callingProcess.NumFrames = frameCount;
            _callingProcess.NotTracked = _notTracked;
            _callingProcess.Tracked = _tracked;
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
                point = _mapper.MapCameraPointToColorSpace(joint.Position);
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
                    point = _mapper.MapCameraPointToColorSpace(joint.Position);
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
            DrawEllipse(ref head, 25);
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
            _canvas.Children.Add(el);
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

            if (_canvas != null)
            {
                _canvas.Children.Add(line);
            }
        }
    }
}
