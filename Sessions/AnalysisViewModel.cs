using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Kinect;
using System.Collections.ObjectModel;
using Microsoft.Kinect.Tools;
using System.Threading;

namespace Sessions
{
    /// <summary>
    /// This is the class view model that works with (the background manager) the UserControl AnalysisView.xaml
    /// 
    /// 
    /// Analysis View Model is a class that handles the clips of executed tests showing joints position as well as
    /// images of the depth view and drawing of skeleton positions.
    /// 
    /// A session must had been selected previously in order to determine which set of clip files will be played/analysed.
    ///
    /// Additionally, user's anthropometric data measured manually may be used for filtering and/or
    /// to better off accuracy and precision.
    /// 
    /// Calibration data is recommended to be used, then calibration process should have had been performed. 
    /// </summary>
    class AnalysisViewModel : ObservableObject, IPageViewModel
    {
        /// <summary>
        /// Pointer for the kinect sensor
        /// </summary>
        private KinectSensor _sensor = null;

        /// <summary>
        /// Pointer for kinect body view that handles body view frames and show them in canvas.
        /// </summary>
        private KinectBodyView kinectBodyView = null;

        /// <summary>
        /// Pointer for kinect IR view that handles IR image
        /// </summary>
        private KinectIRView kinectIRView = null;

        /// <summary>
        /// Assyncronous delegate function for playing video clips
        /// </summary>
        /// <param name="videos"></param>
        private delegate void OneArgDelegate(ObservableCollection<VideoModel> videos);

        /// <summary>
        /// A reference for the Application View Model that manages the whole application it allows access to
        /// global variables/functions.
        /// </summary>
        private ApplicationViewModel _app = null;

        /// <summary>
        /// Identifier of the selected session. A session is a group of video clips related to a user, date and time session.
        /// </summary>
        private int _selectedId = 0;

        /// <summary>
        /// The class object to hold session information
        /// </summary>
        private SessionModel _session;

        /// <summary>
        /// Session view model object to manage session information (like load and save information, i.e.)
        /// </summary>
        private SessionViewModel _sessionVM;

        /// <summary>
        /// Pointers to canvas on the object view (AnalysisView) where kinect streams will show bitmap images or
        /// draw skeletons.
        /// </summary>
        private Canvas _canvasSkeleton;

        /// <summary>
        /// Holds execution status of the playing.
        /// </summary>
        private string _executionStatus;

        /// <summary>
        /// Holds infomration of the joint used for calibration.
        /// </summary>
        private Vector3 _calibrationJoint;

        /// <summary>
        /// Number of read frames.
        /// </summary>
        private Int64 _numFrames = 0;

        /// <summary>
        /// Number of not tracked joint frames.
        /// </summary>
        private Int64 _notTracked = 0;

        /// <summary>
        /// Number of tracked joint frames
        /// </summary>
        private Int64 _tracked = 0;

        /// <summary>
        /// Name of the user being analysed during the session
        /// </summary>
        private string _selectedName;

        /// <summary>
        /// Flag, if true, calibration was done. Then, it has calibration data. Otherwise, there is not.
        /// </summary>
        private bool _hasCalibration;

        /// <summary>
        /// Flag to keep track is the video clip is playing.
        /// </summary>
        private bool _isPlaying = false;

        /// <summary>
        /// Flag to keep track if the video clip has been stopped.
        /// </summary>
        private bool _stopPlaying = false;

        /// <summary>
        /// Object that will treat kinect frames processing filters, selecting the skeleton and reading raw information.
        /// </summary>
        private SessionAnalysisKinect _analysis;

        /// <summary>
        /// List to hold all joints positions for all frames. 
        /// </summary>
        private List<CameraSpacePoint[]> _records;

        /// <summary>
        /// Methods that handle (start and stop) commands when related buttons are pressed on the view.
        /// </summary>
        private ICommand _startCommand;
        private ICommand _stopCommand;

        #region Constructors

        /// <summary>
        /// This empty constructor is necessary to use this object within a UserControl data template.
        /// </summary>
        public AnalysisViewModel()
        {
        }

        /// <summary>
        /// Controls exercise test process online or offline.
        /// </summary>
        /// <param name="app">Pointer to application controller and interface to access other modules</param>
        public AnalysisViewModel(ApplicationViewModel app) 
        {
            // Gets application pointer.
            _app = app;

            // Gets Kinect sensor reference.
            _sensor = KinectSensor.GetDefault();

            // If there is an active kinect / of accessible studio library.
            if (_sensor != null)
            {
                // Opens the sensor.
                _sensor.Open();
            }

            // Updates properties with context information.
            LoadExecutionModel();
        }

        #endregion // Constructors

        /// <summary>
        /// Identifies PageViewModel.
        /// </summary>
        public string Name
        {
            get { return "Analysis View"; }
        }

        /// <summary>
        /// Gets/sets Session name
        /// </summary>
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

        /// <summary>
        /// Gets/sets calibration flag status
        /// </summary>
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
        /// Gets/sets status bar information.
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

        /// <summary>
        /// Gets/sets list of joint position in real world (CameraSpacePoints) records.
        /// </summary>
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
                        param => ! _isPlaying);
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
                    _stopCommand = new RelayCommand(param => StopPlay(), param => _isPlaying);
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
        /// Gets a pointer to kinect body view reader. Used for capturing datacontext in UI elements.
        /// </summary>
        /// <returns></returns>
        public KinectBodyView GetKinectBodyView()
        {
            return kinectBodyView;
        }


        /// <summary>
        /// Gets a pointer to kinect IR view reader. Used for capturing datacontext in UI elements.
        /// </summary>
        /// <returns></returns>
        public KinectIRView GetKinectIRView()
        {
            return kinectIRView;
        }


        /// <summary>
        /// Gets the name of the joint used for calibration
        /// </summary>
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

        /// <summary>
        /// Gets/sets calibration joint position information.
        /// </summary>
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

        /// <summary>
        /// Gets/sets the number of read frames.
        /// </summary>
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

        /// <summary>
        /// Gets/sets the number of not tracked joint frames. Note that for each frame there may be many not tracked joints.
        /// </summary>
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

        /// <summary>
        /// Gets/sets the number of tracked joint frames. Note that for each frame there may be many tracked joints.
        /// </summary>
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


        public bool IsStopped
        {
            get { return _stopPlaying; }
        }


        /// <summary>
        /// Loads specific information of the selected session. It is executed when the instance of the object
        /// is created and every time the User Control View is selected.
        /// </summary>
        public void LoadExecutionModel()
        {
            // Gets the global pointer for the selected session identifier (if any)
            _selectedId = _app.SessionsViewModel.SelectedSessionId;

            // If there is a selected session, loads it, everytime the tab is selected.
            // We don't know if the user of the application selected another sesssion.
            if (_selectedId > 0)
            {
                _sessionVM = new SessionViewModel(_selectedId);
                _session = _sessionVM.Session;

                SelectedName = _session.SessionName;
                HasCalibration = _session.Calibration != null && _session.Calibration.Position != null ? true : false;

                // Instantiate a body view which will show a skeleton based on the depth and body index images.
                kinectBodyView = new KinectBodyView(_sensor, _session.Calibration);

                // Instantiate an IR view which will show Infrared captured image.
                kinectIRView = new KinectIRView(_sensor);
            }
            else
            {
                SelectedName = "Selected name not defined.";
            }

            // Initialize list of read joint information (to be saved or used for statistical analysis in other classes.
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

                // Send the video list to be played.
                OneArgDelegate playback = new OneArgDelegate(PlaybackClip);
                _isPlaying = true;
                _stopPlaying = false;
                playback.BeginInvoke(_session.VideoList, null, null);

                // Instantiate the object that will analysed read frames appropriately.
                _analysis = new SessionAnalysisKinect(_sensor, _session.Calibration, CanvasSkeleton, this);
            }
        }

        /// <summary>
        /// Stops playing the video clip (when the user pressed a stop button on the interface screen).
        /// </summary>
        private void StopPlay()
        {
            int count = -1;
            count = Records.Count;
            _stopPlaying = true;
            _isPlaying = false;
            ExecutionStatus = "Stopped";
        }

        /// <summary>
        /// Playback a list of clips
        /// </summary>
        /// <param name="videos">ObservableCollection of VideoModel objects that contains a filename list of the clips to be played.</param>
        private void PlaybackClip(ObservableCollection<VideoModel> videos)
        {
            // Start a connection to read information from a file or kinect sensor.
            using (KStudioClient client = KStudio.CreateClient())
            {
                VideoModel video;                   // An object to holds information of the video clip.
                client.ConnectToService();
                KStudioEventStreamSelectorCollection streamCollection = new KStudioEventStreamSelectorCollection();
                int i = 0;

                // For each video in the list of videos do:
                while (i < videos.Count)
                {
                    video = videos[i];

                    if (!string.IsNullOrEmpty(video.Filename))
                    {
                        // "Playing " + video.Filename, i, videos.Count

                        using (KStudioPlayback playback = client.CreatePlayback(video.Filename))
                        {
                            // We can use playback.StartPaused() and SeekRelativeTime to start the clip at
                            // a specific time.
                            playback.Start();

                            while (playback.State == KStudioPlaybackState.Playing && ! IsStopped)
                            {
                                Thread.Sleep(500);

                                if(IsStopped)
                                {
                                    break;
                                }
                            }

                            if (playback.State == KStudioPlaybackState.Playing)
                            {
                                playback.Stop();
                            }

                            _isPlaying = false;

                            if (_stopPlaying)
                            {
                                client.DisconnectFromService();
                                return;
                            }
                        }
                    }
                    i++;
                }
                client.DisconnectFromService();
            }
        }
    }
}
