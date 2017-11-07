using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

using Microsoft.Kinect;

namespace Sessions
{
    /// <summary>
    /// Author: Antonio Iyda Paganelli (just adapted the code)
    /// 
    /// A view to show kinect body view data.
    /// </summary>
    class KinectBodyView : ObservableObject
    {
        private KinectSensor sensor = null;

        /// <summary> Reader for body frames </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary> Array for the bodies (Kinect will track up to 6 people simultaneously) </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 10;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        private TimeSpan lastFrame = new TimeSpan();
        private TimeSpan currentFrame = new TimeSpan();
        private TimeSpan elapsedTime;
        private int countMissingFrames = 0;

        private CalibrationModel _calib = null;

        private Vector3 _hipLeft;
        private Vector3 _hipRight;
        private Vector3 _kneeLeft;
        private Vector3 _kneeRight;
        private Vector3 _ankleLeft;
        private Vector3 _ankleRight;

        private double _leftThighLength;
        private double _leftShankLength;
        private double _rightThighLength;
        private double _rightShankLength;

        private CameraSpacePoint _p;
        private float _Xthshd = 0.15f;                   // Threshold from calibration data
        private float _Ythshd = 0.1f;                    // Threshold from calibration data
        private float _Zthshd = 0.1f;                    // Threshold from calibration data

        // HOLT double exponential filter reference
        FilterHoltDouble HoltFilter = null;
        CameraSpacePoint[] holtJoints;
        ColorSpacePoint[] historyTrackedJoints = new ColorSpacePoint[Body.JointCount];

        private DrawingGroup dgHolt;
        private DrawingImage imageSourceHolt;

        public KinectBodyView()
        {
        }

        /// <summary>
        /// Initializes a new instance of KinectBodyView class.
        /// </summary>
        /// <param name="kinectSensor">Active instance of the Kinect Sensor</param>
        /// <param name="calibrationData">Calibration information</param>
        public KinectBodyView(KinectSensor kinectSensor, CalibrationModel calibrationData)
        {
            if (kinectSensor == null)
            {
                throw new ArgumentNullException("kinectSensor");
            }

            sensor = kinectSensor;
            ResetTimers();

            if (calibrationData != null)
            {
                _calib = calibrationData;
                _Xthshd = _calib.Threshold.X;
                _Ythshd = _calib.Threshold.Y;
                _Zthshd = _calib.Threshold.Z;
            } else
            {
                _calib = new CalibrationModel()
                {
                    CalSessionId = 0,
                    JointType = JointType.SpineBase,
                    Position = new Vector3(0f, 0f, 0f)
                };
            }

            HoltFilter = new FilterHoltDouble();
            HoltFilter.Init(0.5f);

            CreateBones();
        }

        /// <summary>
        /// Initializes a new instance of the KinectBodyView class
        /// </summary>
        /// <param name="kinectSensor">Active instance of the KinectSensor</param>
        public KinectBodyView(KinectSensor kinectSensor)
        {
            if (kinectSensor == null)
            {
                throw new ArgumentNullException("kinectSensor");
            }

            sensor = kinectSensor;
            ResetTimers();
            CreateBones();

            _calib = new CalibrationModel()
            {
                CalSessionId = 0,
                JointType = JointType.SpineBase,
                Position = new Vector3(0f, 0f, 0f)
            };
        }

        /// <summary>
        /// Create drawing group and body colors, and body segments.
        /// </summary>
        public void CreateBones()
        {
            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));
            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Create the same for showing Holt filtered image.
            this.dgHolt = new DrawingGroup();
            this.imageSourceHolt = new DrawingImage(this.dgHolt);
        }

        /// <summary>
        /// Reset all counters of the body view and flush old information restarting reader and methods
        /// to receive body frames
        /// </summary>
        public void ResetTimers()
        {
            this.Dispose();

            // open the reader for the body frames
            this.bodyFrameReader = this.sensor.BodyFrameSource.OpenReader();

            this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;

            // get the coordinate mapper
            this.coordinateMapper = this.sensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.sensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            lastFrame = TimeSpan.Zero;
            elapsedTime = new TimeSpan();
            ElapsedTime = TimeSpan.Zero;
            MissingFrames = 0;
        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get { return this.imageSource; }
        }


        public ImageSource ImageSourceHolt
        {
            get { return this.imageSourceHolt; }
        }


        /// <summary>
        /// Gets/sets number of missing frames
        /// </summary>
        public int MissingFrames
        {
            get { return countMissingFrames; }
            set
            {
                if(value != countMissingFrames)
                {
                    countMissingFrames = value;
                    OnPropertyChanged("MissingFrames");
                }
            }
        }

        public TimeSpan ElapsedTime
        {
            get { return elapsedTime; }
            set
            {
                if (elapsedTime != value)
                {
                    elapsedTime = value;
                    OnPropertyChanged("ElapsedTime");
                }
            }
        }

        public Vector3 HipLeft
        {
            get { return _hipLeft; }
            set
            {
                if(value != _hipLeft)
                {
                    _hipLeft = value;
                    OnPropertyChanged("HipLeft");
                }
            }
        }

        public Vector3 HipRight
        {
            get { return _hipRight; }
            set
            {
                if (value != _hipRight)
                {
                    _hipRight = value;
                    OnPropertyChanged("HipRight");
                }
            }
        }

        public Vector3 KneeLeft
        {
            get { return _kneeLeft; }
            set
            {
                if (value != _kneeLeft)
                {
                    _kneeLeft = value;
                    OnPropertyChanged("KneeLeft");
                }
            }
        }

        public Vector3 KneeRight
        {
            get { return _kneeRight; }
            set
            {
                if (value != _kneeRight)
                {
                    _kneeRight = value;
                    OnPropertyChanged("KneeRight");
                }
            }
        }

        public Vector3 AnkleLeft
        {
            get { return _ankleLeft; }
            set
            {
                if (value != _ankleLeft)
                {
                    _ankleLeft = value;
                    OnPropertyChanged("AnkleLeft");
                }
            }
        }

        public Vector3 AnkleRight
        {
            get { return _ankleRight; }
            set
            {
                if (value != _ankleRight)
                {
                    _ankleRight = value;
                    OnPropertyChanged("AnkleRight");
                }
            }
        }

        public double LeftShankLength
        {
            get { return _leftShankLength; }
            set
            {
                if(_leftShankLength != value)
                {
                    _leftShankLength = value;
                    OnPropertyChanged("LeftShankLength");
                }
            }
        }

        public double LeftThighLength
        {
            get { return _leftThighLength; }
            set
            {
                if (_leftThighLength != value)
                {
                    _leftThighLength = value;
                    OnPropertyChanged("LeftThighLength");
                }
            }
        }

        public double RightShankLength
        {
            get { return _rightShankLength; }
            set
            {
                if (_rightShankLength != value)
                {
                    _rightShankLength = value;
                    OnPropertyChanged("RightShankLength");
                }
            }
        }

        public double RightThighLength
        {
            get { return _rightThighLength; }
            set
            {
                if (_rightThighLength != value)
                {
                    _rightThighLength = value;
                    OnPropertyChanged("RightThighLength");
                }
            }
        }

        /// <summary>
        /// Disposes the BodyFrameReader
        /// </summary>
        public void Dispose()
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived -= Reader_BodyFrameArrived;
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor and updates the associated gesture detector object for each body
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        // creates an array of 6 bodies, which is the max number of bodies that Kinect can track simultaneously
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;

                    ProcessMissingFrames(bodyFrame.RelativeTime);
                }
            }

            if (dataReceived)
            {
                // visualize the new body data
                this.UpdateBodyFrame(this.bodies);
            }
        }


        /// <summary>
        /// Calculated based on the elapsed time how many frames are missing since last processed frame.
        /// It is expected to receive a new frame every 33.35 ms (30 fps), tolerable up to every 35 ms.
        /// </summary>
        /// <param name="pElapsed">Relative TimeSpan time of the frame received.</param>
        private void ProcessMissingFrames(TimeSpan pElapsed)
        {
            ElapsedTime = pElapsed;

            // Estimate the number of missing frames recording at 30 fps rate.
            if (lastFrame == TimeSpan.Zero)
            {
                lastFrame = pElapsed;
            }
            else
            {
                currentFrame = pElapsed;
                double diff = currentFrame.TotalMilliseconds - lastFrame.TotalMilliseconds;

                if (diff > 35.0)
                {
                    MissingFrames += (int)Math.Ceiling(diff / 33.333);
                }
                lastFrame = currentFrame;
            }
        }

        /// <summary>
        /// Updates the body array with new information from the sensor
        /// Should be called whenever a new BodyFrameArrivedEvent occurs
        /// </summary>
        /// <param name="bodies">Array of bodies to update</param>
        public void UpdateBodyFrame(Body[] bodies)
        {
            if (bodies != null)
            {
                using (DrawingContext dcHolt = this.dgHolt.Open())
                {
                    using (DrawingContext dc = this.drawingGroup.Open())
                    {
                        // Draw a transparent background to set the render size
                        dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                        dcHolt.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                        int penIndex = 0;
                        foreach (Body body in bodies)
                        {
                            Pen drawPen = this.bodyColors[penIndex++];

                            if (body.IsTracked)
                            {
                                // Gets raw position information from chosen calibration joint.
                                _p = body.Joints[_calib.JointType].Position;

                                // Check if calibration joint position is within expected individual position.
                                if ((_calib.Position.X == 0 || _p.X >= (_calib.Position.X - _Xthshd) && _p.X <= (_calib.Position.X + _Xthshd)) &&
                                   (_calib.Position.Y == 0 || _p.Y >= (_calib.Position.Y - _Ythshd) && _p.Y <= (_calib.Position.Y + _Ythshd)) &&
                                   (_calib.Position.Z == 0 || _p.Z >= (_calib.Position.Z - _Zthshd) && _p.Z <= (_calib.Position.Z + _Zthshd)))
                                {
                                    IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                                    UpdateJointPosition(joints);
                                    CheckLimbLengths(joints);

                                    if (HoltFilter != null)
                                    {
                                        HoltFilter.UpdateFilter(body);
                                        holtJoints = HoltFilter.GetFilteredJoints();
                                    }

                                    // convert the joint points to depth (display) space
                                    Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();
                                    Dictionary<JointType, Point> jointPointsHolt = new Dictionary<JointType, Point>();

                                    foreach (JointType jointType in joints.Keys)
                                    {
                                        // sometimes the depth(Z) of an inferred joint may show as negative
                                        // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                        CameraSpacePoint position = joints[jointType].Position;

                                        if (HoltFilter != null)
                                        {
                                            CameraSpacePoint posHolt = holtJoints[(int)jointType];
                                            if (posHolt.Z < 0)
                                            {
                                                posHolt.Z = InferredZPositionClamp;
                                            }

                                            DepthSpacePoint depthSPHolt = coordinateMapper.MapCameraPointToDepthSpace(posHolt);
                                            jointPointsHolt[jointType] = new Point(depthSPHolt.X, depthSPHolt.Y);

                                        }

                                        if (position.Z < 0)
                                        {
                                            position.Z = InferredZPositionClamp;
                                        }

                                        DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                        jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                                    }

                                    this.DrawBody(joints, jointPoints, dc, drawPen, true);

                                    if (HoltFilter != null)
                                    {
                                        this.DrawBody(joints, jointPointsHolt, dcHolt, drawPen, false);
                                    }

                                    this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                                    this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                                }
                            }
                        }

                        // prevent drawing outside of our render area
                        this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                        this.dgHolt.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                    }
                }
            }
        }

        /// <summary>
        /// Shows on the interface the position of the joints
        /// </summary>
        /// <param name="joints">Body joints to be shown</param>
        private void UpdateJointPosition(IReadOnlyDictionary<JointType, Joint> joints)
        {            
            HipLeft = new Vector3(joints[JointType.HipLeft].Position.X, joints[JointType.HipLeft].Position.Y, joints[JointType.HipLeft].Position.Z);
            HipRight = new Vector3(joints[JointType.HipRight].Position.X, joints[JointType.HipRight].Position.Y, joints[JointType.HipRight].Position.Z);
            KneeRight = new Vector3(joints[JointType.KneeRight].Position.X, joints[JointType.KneeRight].Position.Y, joints[JointType.KneeRight].Position.Z);
            KneeLeft = new Vector3(joints[JointType.KneeLeft].Position.X, joints[JointType.KneeLeft].Position.Y, joints[JointType.KneeLeft].Position.Z);
            AnkleLeft = new Vector3(joints[JointType.AnkleLeft].Position.X, joints[JointType.AnkleLeft].Position.Y, joints[JointType.AnkleLeft].Position.Z);
            AnkleRight = new Vector3(joints[JointType.AnkleRight].Position.X, joints[JointType.AnkleRight].Position.Y, joints[JointType.AnkleRight].Position.Z);
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen, bool trackState)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen, trackState);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen, bool track)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (track && (
                joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked))
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;
                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;
                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Receives the joint position list and verify the lengths of lower limb segments.
        /// </summary>
        /// <param name="joints">Joint list </param>
        private void CheckLimbLengths(IReadOnlyDictionary<JointType, Joint> joints)
        {
            // Gets right limb segment lengths
            if (joints[JointType.HipRight].TrackingState == TrackingState.Tracked &&
               joints[JointType.KneeRight].TrackingState == TrackingState.Tracked &&
               joints[JointType.AnkleRight].TrackingState == TrackingState.Tracked)
            {
                var s = joints[JointType.HipRight].Position;
                var d = joints[JointType.KneeRight].Position;

                // The distance Z given by Kinect represents the distance of the object to the plane of the sensor,
                // not the center of the len. That is why we calculate the distance of the center of the len and its difference
                // to be in the same scale of the other two axis (X and Y).
                RightThighLength = Math.Round(Math.Sqrt(Math.Pow(d.X - s.X, 2) + Math.Pow(d.Y - s.Y, 2) + Math.Pow(Util.Length(d) - Util.Length(s), 2)), 3);

                s = joints[JointType.AnkleRight].Position;
                RightShankLength = Math.Round(Math.Sqrt(Math.Pow(d.X - s.X, 2) + Math.Pow(d.Y - s.Y, 2) + Math.Pow(Util.Length(d) - Util.Length(s), 2)), 3);
            }

            // Gets left limb segment lengths
            if (joints[JointType.HipLeft].TrackingState == TrackingState.Tracked &&
                joints[JointType.KneeLeft].TrackingState == TrackingState.Tracked &&
                joints[JointType.AnkleLeft].TrackingState == TrackingState.Tracked)
            {
                var s = joints[JointType.HipLeft].Position;
                var d = joints[JointType.KneeLeft].Position;

                LeftThighLength = Math.Round(Math.Sqrt(Math.Pow(d.X - s.X, 2) + Math.Pow(d.Y - s.Y, 2) + Math.Pow(Util.Length(d) - Util.Length(s), 2)), 3);

                s = joints[JointType.AnkleLeft].Position;
                LeftShankLength = Math.Round(Math.Sqrt(Math.Pow(d.X - s.X, 2) + Math.Pow(d.Y - s.Y, 2) + Math.Pow(Util.Length(d) - Util.Length(s), 2)), 3);
            }
        }
    }
}
