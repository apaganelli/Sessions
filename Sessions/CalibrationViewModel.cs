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

        private int _selectedId = 0;                            // Selected session ID for running got from _app
        private int _numFrames = 0;
        private string _selectedJoint = null;
        private int _selectedJointIndex = 0;

        private Vector3 _calibrationResult;
        JointType _jointType = JointType.SpineBase;
        private int _processedFrames = 0;
        private string _calibrationStatus = "Configuring ...";

        private SessionViewModel session = null;

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

            LoadCalibrationData();
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


        /// <summary>
        /// Load calibration data of the selected ID. (if a calibration had already been performed.)
        /// Returns the filename of the selected video for calibration
        /// Update calibration information of calibration view model.
        /// </summary>
        public string LoadCalibrationData()
        {
            string filename = null;

            // This attribution is not inside the constructor method because it may be called every time the page is loaded.
            SessionsViewModel sessionsCtrl = (SessionsViewModel)_app.PageViewModels[0];
            _selectedId = sessionsCtrl.SelectedSessionId;

            if (_selectedId > 0)
            {
                CalibrationStatus = "Configuring ....";
                ProcessedFrames = 0;
                CalibrationResult = new Vector3(0, 0, 0);

                var page = _app.PageViewModels.Where(p => p.Name == "Session View");

                // If exist already an instance of SessionViewModel, then use it, or instantiate one.
                if (page.Any())
                {
                    session = (SessionViewModel)page;
                }
                else
                {
                    session = new SessionViewModel(_app);
                }

                // Gets persistent Session information.
                xmlSessionDoc = session.XmlSessionDoc;

                // Find the selected session in order to pick up the calibration video.
                string xpath = "/Sessions/Session[@Id='{0}']";
                xpath = String.Format(xpath, _selectedId);
                xNode = xmlSessionDoc.DocumentElement.SelectSingleNode(xpath);

                if (xNode != null)
                {
                    foreach (XmlNode child in xNode)
                    {
                        if (child.Name == "Video")
                        {
                            if (child.Attributes["Calibration"].Value == "True")
                            {
                                filename = child.InnerText;
                            }
                        } else if(child.Name == "Calibration")
                        {
                            _jointType = (JointType)Int32.Parse(child.Attributes["JointType"].Value);
                            NumFrames = Int32.Parse(child.Attributes["NumFrames"].Value);
                            float x, y, z;

                            x = float.Parse(child.Attributes["X"].Value);
                            y = float.Parse(child.Attributes["Y"].Value);
                            z = float.Parse(child.Attributes["Z"].Value);

                            CalibrationResult = new Vector3(x, y, z);

                            // Update currentSession information?
                        }
                    }
                }
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
            session.SaveCalibrationData(xNode, calibration);

            // Update current session calibration values.
            _app.SessionsViewModel.CurrentSession.Calibration = calibration;
            MessageBox.Show("Calibration saved.", "Calibration", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RunCalibration()
        {
            string filename = null;

            if (_selectedId > 0)
            {
                filename = LoadCalibrationData();

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

                SessionKinect runVideo = null;
                runVideo = new SessionKinect(this, filename, NumFrames, _jointType);
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
