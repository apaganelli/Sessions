using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Kinect;


namespace Sessions
{
    /// <summary>
    /// Execution View Model is a class that manages the test execution where joints position data
    /// will be collected and organized for future analysis.
    /// 
    /// A session must be selected in order to determine what set of video files will be played.
    /// Additionally, data from the user like anthropometric measures may be used for filtering and/or
    /// to better off accuracy and precision.
    /// 
    /// Calibration data is recommended to be used, then calibration process should have had been performed.
    /// 
    /// </summary>
    class AnalysisViewModel : ObservableObject, IPageViewModel
    {
        private ApplicationViewModel _app = null;

        private int _selectedId = 0;
        private SessionModel _session;
        private SessionViewModel _sessionVM;

        private Canvas _canvasSkeleton;
        private Canvas _canvasImage;
        private Canvas _filteredCanvas;

        private string _executionStatus;

        private Vector3 _hipRightPosition;
        private Vector3 _kneeRightPosition;
        private Vector3 _ankleRightPosition;

        private Vector3 _hipLeftPosition;
        private Vector3 _kneeLeftPosition;
        private Vector3 _ankleLeftPosition;

        private Vector3 _calibrationJoint;

        private double _leftThighLength;
        private double _leftShankLength;
        private double _rightThighLength;
        private double _rightShankLength;

        private Int64 _numFrames = 0;
        private Int64 _notTracked = 0;
        private Int64 _tracked = 0;

        private string _selectedName;
        private bool _hasCalibration;
        private bool _play = false;

        private SessionAnalysisKinect _analysis;

        private List<CameraSpacePoint[]> _records;

        private ICommand _startCommand;
        private ICommand _stopCommand;

        #region Constructors
        /// <summary>
        /// Controls exercise test process online or offline.
        /// </summary>
        /// <param name="app">Pointer to application controller and interface to access other modules</param>
        public AnalysisViewModel(ApplicationViewModel app) 
        {
            _app = app;
            LoadExecutionModel();
        }

        #endregion // Constructors

        /// <summary>
        /// Identifies PageViewModel.
        /// </summary>
        public string Name
        {
            get { return "Execution View"; }
        }

        public string SelectedName
        {
            get { return _selectedName; }
            set
            {
                if(value != _selectedName)
                {
                    _selectedName = value;
                    OnPropertyChanged("SelectedName");
                }
            }
        }

        public bool HasCalibration
        {
            get { return _hasCalibration; }
            set
            {
                if(value != _hasCalibration)
                {
                    _hasCalibration = value;
                    OnPropertyChanged("HasCalibration");
                }
            }
        }

        /// <summary>
        /// Status bar for giving feedback to user.
        /// </summary>
        public string ExecutionStatus
        {
            get { return _executionStatus; }
            set
            {
                if (value != _executionStatus)
                {
                    _executionStatus = value;
                    OnPropertyChanged("ExecutionStatus");
                }
            }
        }

        public double LeftThighLength
        {
            get { return _leftThighLength; }
            set
            {
                if(value != _leftThighLength)
                {
                    _leftThighLength = value;
                    OnPropertyChanged("LeftThighLength");
                }
            }
        }

        public double RightThighLength
        {
            get { return _rightThighLength; }
            set
            {
                if (value != _rightThighLength)
                {
                    _rightThighLength = value;
                    OnPropertyChanged("RightThighLength");
                }
            }
        }

        public double LeftShankLength
        {
            get { return _leftShankLength; }
            set
            {
                if (value != _leftShankLength)
                {
                    _leftShankLength = value;
                    OnPropertyChanged("LeftShankLength");
                }
            }
        }

        public double RightShankLength
        {
            get { return _rightShankLength; }
            set
            {
                if (value != _rightShankLength)
                {
                    _rightShankLength = value;
                    OnPropertyChanged("RightShankLength");
                }
            }
        }

        public List<CameraSpacePoint[]> Records
        {
            get { return _records; }
            set {
                if(value != _records)
                {
                    _records = value;
                }
            }
        }

        /// <summary>
        /// Relay command to start video playback.
        /// </summary>
        public ICommand StartCommand
        {
            get
            {
                if (_startCommand == null)
                {
                    _startCommand = new RelayCommand(param => StartPlay(),
                        param => true);
                }
                return _startCommand;
            }

        }

        /// <summary>
        /// Relay command to stop video playback
        /// </summary>
        public ICommand StopCommand
        {
            get
            {
                if (_stopCommand == null)
                {
                    _stopCommand = new RelayCommand(param => StopPlay(), param => _play);
                }
                return _stopCommand;
            }
        }

        /// <summary>
        /// Pointer to the UI canvas where the skeleton will be shown.
        /// </summary>
        public Canvas CanvasSkeleton
        {
            get { return _canvasSkeleton; }
            set
            {
                if(value != _canvasSkeleton)
                {
                    _canvasSkeleton = value;
                    OnPropertyChanged("CanvasSkeleton");
                }
            }
        }

        /// <summary>
        /// Gets/sets Filtered canvas used for showing filtered skeleton.
        /// </summary>
        public Canvas FilteredCanvas
        {
            get { return _filteredCanvas; }
            set
            {
                if (value != _filteredCanvas)
                {
                    _filteredCanvas = value;
                    OnPropertyChanged("FilteredCanvas");
                }
            }
        }

        /// <summary>
        /// Pointer to the UI canvas where video images will appear.
        /// </summary>
        public Canvas CanvasImage
        {
            get { return _canvasImage; }
            set
            {
                if (value != _canvasImage)
                {
                    _canvasImage = value;
                    OnPropertyChanged("CanvasImage");
                }
            }
        }

        /// <summary>
        /// Holds X,Y,Z position of the right hip.
        /// </summary>
        public Vector3 HipRightPosition
        {
            get { return _hipRightPosition; }
            set
            {
                if(value != _hipRightPosition)
                {
                    _hipRightPosition = value;
                    OnPropertyChanged("HipRightPosition");
                }
            }
        }

        /// <summary>
        /// Holds, X,Y,Z position of the right knee.
        /// </summary>
        public Vector3 KneeRightPosition
        {
            get { return _kneeRightPosition; }
            set
            {
                if (value != _kneeRightPosition)
                {
                    _kneeRightPosition = value;
                    OnPropertyChanged("KneeRightPosition");
                }
            }
        }

        /// <summary>
        /// Holds, X,Y,Z position of the right ankle.
        /// </summary>
        public Vector3 AnkleRightPosition
        {
            get { return _ankleRightPosition; }
            set
            {
                if (value != _ankleRightPosition)
                {
                    _ankleRightPosition = value;
                    OnPropertyChanged("AnkleRightPosition");
                }
            }
        }

        /// <summary>
        /// Holds X,Y,Z position of the left hip.
        /// </summary>
        public Vector3 HipLeftPosition
        {
            get { return _hipLeftPosition; }
            set
            {
                if (value != _hipLeftPosition)
                {
                    _hipLeftPosition = value;
                    OnPropertyChanged("HipLeftPosition");
                }
            }
        }

        /// <summary>
        /// Holds, X,Y,Z position of the left knee.
        /// </summary>
        public Vector3 KneeLeftPosition
        {
            get { return _kneeLeftPosition; }
            set
            {
                if (value != _kneeLeftPosition)
                {
                    _kneeLeftPosition = value;
                    OnPropertyChanged("KneeLeftPosition");
                }
            }
        }

        /// <summary>
        /// Holds, X,Y,Z position of the left ankle.
        /// </summary>
        public Vector3 AnkleLeftPosition
        {
            get { return _ankleLeftPosition; }
            set
            {
                if (value != _ankleLeftPosition)
                {
                    _ankleLeftPosition = value;
                    OnPropertyChanged("AnkleLeftPosition");
                }
            }
        }

        public string JointName
        {
            get
            {
                if(HasCalibration && _session != null)
                {
                    return _session.Calibration.JointType.ToString();
                }

                return "None";
            }
        }

        public Vector3 CalibrationJoint
        {
            get { return _calibrationJoint; }
            set
            {
                if(value != _calibrationJoint)
                {
                    _calibrationJoint = value;
                    OnPropertyChanged("CalibrationJoint");
                }
            }
        }

        public Int64 NumFrames
        {
            get { return _numFrames; } 
            set
            {
                if(value != _numFrames)
                {
                    _numFrames = value;
                    OnPropertyChanged("NumFrames");
                }
            }
        }

        public Int64 NotTracked
        {
            get { return _notTracked; }
            set
            {
                if (value != _notTracked)
                {
                    _notTracked = value;
                    OnPropertyChanged("NotTracked");
                }
            }
        }

        public Int64 Tracked
        {
            get { return _tracked; }
            set
            {
                if (value != _tracked)
                {
                    _tracked = value;
                    OnPropertyChanged("Tracked");
                }
            }
        }


        /// <summary>
        /// Loads specific information of the selected session.
        /// </summary>
        public void LoadExecutionModel()
        {
            _selectedId = _app.SessionsViewModel.SelectedSessionId;

            // If there is a selected session, loads it, everytime the tab is selected.
            if (_selectedId > 0)
            {
                _sessionVM = new SessionViewModel(_selectedId);
                _session = _sessionVM.Session;

                SelectedName = _session.SessionName;
                HasCalibration = _session.Calibration != null && _session.Calibration.Position != null ? true : false;
            }
            else
            {
                SelectedName = "Selected name not defined.";
            }

            Records = null;
            Records = new List<CameraSpacePoint[]>();
        }

        /// <summary>
        /// Private function for executing kinect interface.
        /// </summary>
        private void StartPlay()
        {
            if (_session != null)
            {
                ExecutionStatus = "Starting analysis";
                _analysis = new SessionAnalysisKinect(_session.VideoList, _session.Calibration, CanvasImage, 
                                                      CanvasSkeleton, FilteredCanvas, this);
                _play = _analysis.IsPlaying;
            }
        }

        private void StopPlay()
        {
            int count = -1;

            count = Records.Count;

            if (_analysis != null)
            {
                _analysis.Stop = true;
                _analysis.CloseAll();
                _analysis = null;
            }

            ExecutionStatus = "Stopped";
        }
    }
}
