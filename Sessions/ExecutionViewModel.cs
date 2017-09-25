using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

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
    class ExecutionViewModel : ObservableObject, IPageViewModel
    {
        private ApplicationViewModel _app = null;

        private int _selectedId = 0;
        private SessionModel _session;
        private SessionViewModel _sessionVM;

        private Canvas _canvasSkeleton;
        private Canvas _canvasImage;

        private string _executionStatus;

        private Vector3 _hipRightPosition;
        private Vector3 _kneeRightPosition;
        private Vector3 _ankleRightPosition;

        private Vector3 _hipLeftPosition;
        private Vector3 _kneeLeftPosition;
        private Vector3 _ankleLeftPosition;

        private string _selectedName;
        private bool _hasCalibration;
        private bool _play = false;

        private SessionAnalysisKinect _analysis;

        private ICommand _startCommand;
        private ICommand _stopCommand;

        #region Constructors
        /// <summary>
        /// Controls exercise test process online or offline.
        /// </summary>
        /// <param name="app">Pointer to application controller and interface to access other modules</param>
        public ExecutionViewModel(ApplicationViewModel app) 
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

        public void LoadExecutionModel()
        {
            _selectedId = _app.SessionsViewModel.SelectedSessionId;

            if (_selectedId > 0)
            {
                if (_sessionVM == null || (_session != null && _selectedId != _session.SessionId))
                {
                    _sessionVM = new SessionViewModel(_selectedId);
                    _session = _sessionVM.Session;

                    SelectedName = _session.SessionName;
                    HasCalibration = _session.Calibration != null && _session.Calibration.Position != null ? true : false;

                }
            } else
            {
                SelectedName = "Selected name not defined.";
            }
        }

        private void StartPlay()
        {
            if (_session != null)
            {
                ExecutionStatus = "Starting analysis";
                _analysis = new SessionAnalysisKinect(_session.VideoList, _session.Calibration, CanvasImage, CanvasSkeleton, this);
                _play = _analysis.IsPlaying;
            }
        }

        private void StopPlay()
        {
            if (_analysis != null && _analysis.IsPlaying)
            {
                _analysis.Stop = true;
                ExecutionStatus = "Stopped";
            }
        }


    }
}
