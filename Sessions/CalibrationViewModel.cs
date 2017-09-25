/// @Author:    Antonio Iyda Paganelli
/// @Date:      12/09/2017
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;
using Microsoft.Kinect.Tools;

using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using System.Xml;



namespace Sessions
{
    /// <summary>
    /// CalibravionViewModel is a datacontext that controls the calibration process.
    /// If a session was selected, the calibration will run using the video whose Calibration property is true of this
    /// session and it will record the results into xml session entry.
    /// 
    /// If a session was not selected, it will run on the fly over the captured data streams and its calibration
    /// result will be lost.
    /// </summary>
    class CalibrationViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        private ApplicationViewModel _app;
        private ObservableCollection<string> _jointTypes;

        private SessionModel _session = null;

        private int _selectedId = 0;                            // Selected session ID for running got from _app
        private string _selectedName;
        private int _numFrames = 0;
        private string _selectedJoint = null;
        private int _selectedJointIndex = 0;

        private Vector3 _calibrationResult;
        JointType _jointType = JointType.SpineBase;
        private int _processedFrames = 0;
        private string _calibrationStatus = "Configuring ...";

        private SessionViewModel _sessionVM = null;

        /// <summary>
        /// Pointer to xml sessions data file.
        /// </summary>
        XmlDocument xmlSessionDoc;

        /// <summary>
        /// Pointer to selected Sesssion.
        /// </summary>
        XmlNode xNode;

        private ICommand _runCommand;
        private ICommand _saveCalibrationCommand;
#endregion // Fields

        #region Constructor
        public CalibrationViewModel(ApplicationViewModel app)
        {
            // Gets reference to application navigation controller.
            _app = app;

            // Only used to select the joint type.
            _jointTypes = new ObservableCollection<string>();
            _jointTypes.Add("SpineBase");
            _jointTypes.Add("SpineMid");
            _jointTypes.Add("HipRight");
            _jointTypes.Add("HipLeft");
            _jointTypes.Add("Head");

            LoadCalibrationData(false);
        }

        #endregion // Constructor

        #region Properties
        public string Name
        {
            get { return "CalibrationModel"; }
        }

        public ObservableCollection<string> JointTypes
        {
            get { return _jointTypes; }
        }

        /// <summary>
        /// Property to show how many frames were already processed during calibration
        /// </summary>
        public int ProcessedFrames
        {
            get { return _processedFrames; } 
            set
            {
                if(value != _processedFrames)
                {
                    _processedFrames = value;
                    OnPropertyChanged("ProcessedFrames");
                }
            }
        }

        public int NumFrames
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

        public string SelectedJoint
        {
            get { return _selectedJoint; }
            set
            {
                if(value != _selectedJoint)
                {
                    _selectedJoint = value;
                    OnPropertyChanged("SelectedJoint");
                }
            }
        }

        public int SelectedJointIndex
        {
            get { return _selectedJointIndex; }
            set
            {
                if(value != _selectedJointIndex)
                {
                    _selectedJointIndex = value;
                    OnPropertyChanged("SelectedJointIndex");
                }
            }
        }

        public Vector3 CalibrationResult
        {
            get { return _calibrationResult; }
            set
            {
                if(value != _calibrationResult)
                {
                    _calibrationResult = value;
                    OnPropertyChanged("CalibrationResult");
                }
            }
        }

        public string CalibrationStatus
        {
            get { return _calibrationStatus; }
            set
            {
                if(value != _calibrationStatus)
                {
                    _calibrationStatus = value;
                    OnPropertyChanged("CalibrationStatus");
                }
            }
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


        /// <summary>
        /// Load calibration data of the selected ID. (if a calibration had already been performed.)
        /// Returns the filename of the selected video for calibration
        /// Update calibration information of calibration view model.
        /// </summary>
        /// <param name="onlyFilename">Gets only filename or load full information</param>
        public string LoadCalibrationData(bool onlyFilename)
        {
            string filename = null;

            // This attribution is not inside the constructor method because it may be called every time the page is loaded.
            SessionsViewModel sessionsCtrl = (SessionsViewModel)_app.PageViewModels[0];
            _selectedId = sessionsCtrl.SelectedSessionId;

            if (_selectedId > 0)
            {
                var page = _app.PageViewModels.Where(p => p.Name == "Session View");

                // If exist already an instance of SessionViewModel, then use it, or instantiate one.
                if (page.Any())
                {
                    _sessionVM = (SessionViewModel)page;
                }
                else
                {
                    _sessionVM = new SessionViewModel(_app);
                }

                // Gets persistent Session information.
                xmlSessionDoc = _sessionVM.XmlSessionDoc;

                // Find the selected session in order to pick up the calibration video.
                string xpath = "/Sessions/Session[@Id='{0}']";
                xpath = String.Format(xpath, _selectedId);
                xNode = xmlSessionDoc.DocumentElement.SelectSingleNode(xpath);

                if (xNode != null)
                {
                    _session = _sessionVM.LoadSession(xNode);
                    SelectedName = _session.SessionName;

                    foreach (VideoModel video in _session.VideoList)
                    {
                        if (video.IsCalibration)
                        {
                            filename = video.Filename;
                        }
                    }

                    if (_session.Calibration != null && !onlyFilename)
                    {
                        _jointType = _session.Calibration.JointType;
                        NumFrames = _session.Calibration.NumFrames;
                        CalibrationResult = _session.Calibration.Position;
                        ConvertSelectedJointIndex();
                        _app.SessionsViewModel.CurrentSession = _session;
                    }                    
                }
            }

            if(filename == null)
            {
                CalibrationStatus = "Configuring ....";
                ProcessedFrames = 0;
                CalibrationResult = new Vector3(0, 0, 0);
                SelectedJointIndex = 0;
                NumFrames = 0;
            }

            return filename;
        }

        /// <summary>
        /// RelayCommand to enable run button that starts calibration process
        /// </summary>
        public ICommand RunCommand
        {
            get
            {
                if (_runCommand == null)
                {
                    _runCommand = new RelayCommand(param => RunCalibration(), 
                        param => (NumFrames > 0) && SelectedJoint != null);
                }
                return _runCommand;
            }
        }

        public ICommand SaveCalibrationCommand
        {
            get
            {
                if (_saveCalibrationCommand == null)
                {
                    _saveCalibrationCommand = new RelayCommand(param => SaveCalibration(),
                        param => (NumFrames > 0 && ProcessedFrames >= NumFrames));
                }
                return _saveCalibrationCommand;
            }
        }
        #endregion // Properties


        private void ConvertSelectedJointIndex()
        {
            switch (_jointType)
            {
                case JointType.SpineMid:
                    SelectedJointIndex = 1;
                    break;
                case JointType.HipRight:
                    SelectedJointIndex = 2;
                    break;
                case JointType.HipLeft:
                    SelectedJointIndex = 3;
                    break;
                case JointType.Head:
                    SelectedJointIndex = 4;
                    break;
                default:
                    SelectedJointIndex = 0;
                    break;
            }
        }

        /// <summary>
        /// Converts UI selected joint (string) to JointType (Enum - Kinect) and store it into _jointType field.
        /// </summary>
        private void ConvertSelectedJoint()
        {
            switch (SelectedJoint)
            {
                case "SpineMid":
                    _jointType = JointType.SpineMid;
                    break;
                case "HipRight":
                    _jointType = JointType.HipRight;
                    break;
                case "HipLeft":
                    _jointType = JointType.HipLeft;
                    break;
                case "Head":
                    _jointType = JointType.Head;
                    break;
            }
        }

        /// <summary>
        /// Stores Calibration Result into a XML file (probably sessions.xml)
        /// </summary>
        private void SaveCalibration()
        {
            CalibrationModel calibration = new CalibrationModel();
            calibration.CalSessionId = _selectedId;
            ConvertSelectedJoint();
            calibration.JointType = _jointType;
            calibration.NumFrames = NumFrames;
            calibration.Position = CalibrationResult;

            // Session is a member of SessionViewModel class. It was linked during constructor´s call
            // to LoadCalibration data.
            _sessionVM.SaveCalibrationData(xNode, calibration);

            // Update current session calibration values.

            if(_app.SessionsViewModel.CurrentSession != null)
            {
                _app.SessionsViewModel.CurrentSession.Calibration = calibration;
            }

            ProcessedFrames = 0;
            MessageBox.Show("Calibration saved.", "Calibration", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Private function for running calibration processes, starting kinect playback on selected calibration video.
        /// </summary>
        private void RunCalibration()
        {
            string filename = null;

            if (_selectedId > 0)
            {
                filename = LoadCalibrationData(true);

                if (filename == null)
                {
                    // Not found a video name with the flag Calibration equals True.
                    // Then, inform the user the selected session has no selected calibration video.

                    CalibrationStatus = "Not ready";
                    MessageBox.Show("The selected session " + _selectedId + " has no Calibration video selected.\n" +
                        "You should return to Sessions tab, edit the session and select a Calibration video.",
                        "Calibration Video not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Convert UI selected joint (string) to JointType (Enum - Kinect) and store it in _jointType.
                ConvertSelectedJoint();

                SessionCalibrationKinect runVideo = null;
                runVideo = new SessionCalibrationKinect(this, filename, NumFrames, _jointType);
            }
            else
            {
                CalibrationStatus = "Not ready.";
                MessageBox.Show("There is no session selected." +
                    "You should return to Sessions tab and select a Session with Calibration video specified.",
                    "Session NOT Found", MessageBoxButton.OK, MessageBoxImage.Information);
            }            
        } // End of RunCalibration
    }
}
