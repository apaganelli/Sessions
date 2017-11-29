using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;

using Microsoft.Kinect;
using System.Xml.Linq;

namespace Sessions
{
    /// <summary>
    /// Author: Antonio Iyda Paganelli
    /// 
    /// SessionModelView is used to edit or create a new Session SessionModel.
    /// </summary>
    class SessionViewModel : ObservableObject, IPageViewModel
    {
        /// <summary>
        /// Reference to main controller.
        /// </summary>
        private ApplicationViewModel _appViewModel;

        /// <summary>
        /// Holds session information
        /// </summary>
        private SessionModel _session;

        /// <summary>
        /// Points out the last visited page. To return to it on finish.
        /// </summary>
        private IPageViewModel _previousPage;

        /// <summary>
        /// Actions executed from interface.
        /// </summary>
        private ICommand _saveSessionCommand;
        private ICommand _cancelSessionCommand;

        /// <summary>
        /// Internal flag for not reading the session data all the time.
        /// </summary>
        private bool _loaded = false;

        /// <summary>
        /// Holds a list of session data.
        /// </summary>
        private List<SessionModel> _allSessions = new List<SessionModel>();

        /// <summary>
        /// Used for reading the session data file that is stored in XML format.
        /// </summary>
        private XmlDocument _xmlSessionDoc = null;

        /// <summary>
        /// Hold which operation has to be performed.
        /// </summary>
        private string Operation = "Nil";

        private string _buttonText = "Save";

        /// <summary>
        /// Constructor for creating new session entries.
        /// </summary>
        /// <param name="app"></param>
        public SessionViewModel(ApplicationViewModel app)
        {
            _appViewModel = app;
            _previousPage = app.CurrentPageViewModel;
            _session = new SessionModel();
            Operation = "Create";

            LoadSessions();
            int nextId = AllSessions.Count == 0 ? 1 : AllSessions.Last<SessionModel>().SessionId + 1;
            _session.SessionId = nextId;
            _session.VideoList = new ObservableCollection<VideoModel>();
            _session.Calibration = new CalibrationModel();

            _appViewModel.SessionsViewModel.CurrentSession = _session;
        }

        /// <summary>
        /// Constructor for editing or removing an existing session.
        /// </summary>
        /// <param name="app">Application View Model - controller of Navigation</param>
        /// <param name="id">Session ID</param>
        /// <param name="isRemove">Flag: true -> Remove Id, false -> Edit</param>
        public SessionViewModel(ApplicationViewModel app, int id, bool isRemove)
        {
            _appViewModel = app;
            _previousPage = app.CurrentPageViewModel;

            // Finds the session and loads its information onto a local object.
            LoadSessions();
            IEnumerable<SessionModel> s = AllSessions.Where(x => x.SessionId == id);
            _session = s.FirstOrDefault();
            _appViewModel.SessionsViewModel.CurrentSession = _session;

            if (!isRemove)
                Operation = "Edit";
            else
            {
                Operation = "Remove";
                _buttonText = "Confirm?";
            }
        }

        /// <summary>
        /// Constructor for loading a session indexed by id into Session instance.
        /// </summary>
        /// <param name="id">SessionId to be loaded</param>
        public SessionViewModel(int id)
        {
            _xmlSessionDoc = new XmlDocument();
            string xmlFilename = System.Configuration.ConfigurationManager.AppSettings["xmlSessionsFile"];
            _xmlSessionDoc.Load(xmlFilename);

            string xpath = "/Sessions/Session[@Id='{0}']";
            xpath = String.Format(xpath, id);

            XmlNode xNode = null;
            xNode = _xmlSessionDoc.DocumentElement.SelectSingleNode(xpath);

            _session = LoadSession(xNode);
        }

#region Properties
        public string ButtonText
        {
            get { return _buttonText; }
        }

        // IPageViewModel required property.
        public string Name
        {
            get { return "Session View"; }
        }

        /// <summary>
        /// Gets/sets session Id.
        /// </summary>
        public int SessionId
        {
            get { return _session.SessionId; }
            set
            {
                if (value != _session.SessionId) _session.SessionId = value;
            }
        }

        /// <summary>
        /// Gets/sets session name.
        /// </summary>
        public string SessionName
        {
            get { return _session.SessionName; }
            set
            {
                if (value != _session.SessionName) _session.SessionName = value;
            }
        }

        /// <summary>
        /// Gets/sets session date (string format)
        /// </summary>
        public string SessionDate
        {
            get { return _session.SessionDate; }
            set
            {
                if (value != _session.SessionDate)
                {
                    _session.SessionDate = value;
                    OnPropertyChanged("SessionDate");
                }

            }
        }

        /// <summary>
        /// Gets/sets thigh length
        /// </summary>
        public double ThighLength
        {
            get { return _session.ThighLength; }
            set
            {
                if(value != _session.ThighLength)
                {
                    _session.ThighLength = value;
                    OnPropertyChanged("ThighLength");
                }
            }
        }

        /// <summary>
        /// Gets/sets shank length
        /// </summary>
        public double ShankLength
        {
            get { return _session.ShankLength; }
            set
            {
                if (value != _session.ShankLength)
                {
                    _session.ShankLength = value;
                    OnPropertyChanged("ShankLength");
                }
            }
        }

        /// <summary>
        /// Gets/sets modality (running, cycling, rowing, free style etc)
        /// </summary>
        public string Modality
        {
            get { return _session.Modality; }
            set
            {
                if (value != _session.Modality)
                {
                    _session.Modality = value;
                    OnPropertyChanged("Modality");
                }
            }
        }

        /// <summary>
        /// Gets / Sets session type: continous, progressive, interval, random/fartlek.
        /// </summary>
        public string SessionType
        {
            get { return _session.SessionType; }
            set
            {
                if (value != _session.SessionType)
                {
                    _session.SessionType = value;
                    OnPropertyChanged("SessionType");
                }
            }
        }

        /// <summary>
        /// Gets / sets list of video clips associated to session
        /// </summary>
        public ObservableCollection<VideoModel> VideoList
        {
            get { return _session.VideoList; }
            set
            {
                if (value != _session.VideoList)
                {
                    _session.VideoList = value;
                    OnPropertyChanged("VideoList");
                }
            }
        }

        /// <summary>
        /// Gets calibration object.
        /// </summary>
        public CalibrationModel Calibration
        {
            get { return _session.Calibration; }
        }

        /// <summary>
        /// Gets session object.
        /// </summary>
        public SessionModel Session
        {
            get { return _session; }
        }
        #endregion

        #region commands
        public ICommand CancelSessionCommand
        {
            get
            {
                if (_cancelSessionCommand == null)
                {
                    _cancelSessionCommand = new RelayCommand(param => CancelSession());
                }
                return _cancelSessionCommand;
            }
        }

        public ICommand SaveSessionCommand
        {
            get
            {
                if (_saveSessionCommand == null)
                {
                    _saveSessionCommand = new RelayCommand(param => SaveSession(), 
                        param => ((_session.SessionName != "") &&
                        (_session.SessionDate != "")));
                }
                return _saveSessionCommand;
            }
        }

#endregion


        public List<SessionModel> AllSessions
        {
            get { return _allSessions; }
        }

        public XmlDocument XmlSessionDoc
        {
            get { return _xmlSessionDoc; }
            set
            {
                if (value != _xmlSessionDoc)
                {
                    _xmlSessionDoc = value;
                }
            }
        }

        /// <summary>
        /// Loads Xml data of a specific node into SessionModel instance.
        /// </summary>
        /// <param name="node">XMLNode to be loaded int SessionModel instance</param>
        /// <returns></returns>
        public SessionModel LoadSession(XmlNode node)
        {
            SessionModel s = new SessionModel();
            s.SessionId = Int32.Parse(node.Attributes["Id"].Value);
            s.SessionName = node.Attributes["Name"].Value;
            s.ThighLength = Double.Parse(node.Attributes["Thigh"].Value);
            s.ShankLength = Double.Parse(node.Attributes["Shank"].Value);
            s.SessionDate = node.Attributes["Date"].Value;
            s.Modality = node.Attributes["Modality"].Value;
            s.SessionType = node.Attributes["Type"].Value;
            s.VideoList = new ObservableCollection<VideoModel>();

            foreach (XmlNode child in node)
            {
                // Has to check if child is not Calibration
                if (child.Name == "Video")
                {
                    VideoModel video = new VideoModel();

                    video.Sequence = Int32.Parse(child.Attributes["Sequence"].Value);
                    video.Power = Int32.Parse(child.Attributes["Power"].Value);

                    if (child.Attributes["Calibration"].Value == "True")
                    {
                        video.IsCalibration = true;
                    }
                    else
                    {
                        video.IsCalibration = false;
                    }

                    video.Filename = child.InnerText;

                    s.VideoList.Add(video);
                }
                else if (child.Name == "Calibration")
                {
                    if (s.Calibration == null)
                    {
                        s.Calibration = new CalibrationModel();
                    }
                    s.Calibration.CalSessionId = s.SessionId;
                    s.Calibration.NumFrames = Int32.Parse(child.Attributes["NumFrames"].Value);
                    s.Calibration.JointType = (JointType)Int32.Parse(child.Attributes["JointType"].Value);

                    s.Calibration.InitialTime = Int64.Parse(child.Attributes["InitialTime"].Value);

                    // Gets calibration information
                    float x, y, z;
                    x = float.Parse(child.Attributes["X"].Value);
                    y = float.Parse(child.Attributes["Y"].Value);
                    z = float.Parse(child.Attributes["Z"].Value);
                    s.Calibration.Position = new Vector3(x, y, z);

                    // Gets threshold information
                    x = float.Parse(child.Attributes["TX"].Value);
                    y = float.Parse(child.Attributes["TY"].Value);
                    z = float.Parse(child.Attributes["TZ"].Value);
                    s.Calibration.Threshold = new Vector3(x, y, z);

                    // Gets standard deviation information
                    x = float.Parse(child.Attributes["SDX"].Value);
                    y = float.Parse(child.Attributes["SDY"].Value);
                    z = float.Parse(child.Attributes["SDZ"].Value);
                    s.Calibration.SD = new Vector3(x, y, z);

                    // Gets estimated initial joint position information
                    x = float.Parse(child.Attributes["EX"].Value);
                    y = float.Parse(child.Attributes["EY"].Value);
                    z = float.Parse(child.Attributes["EZ"].Value);
                    s.Calibration.Estimated = new Vector3(x, y, z);

                    // Gets segments lengths
                    s.Calibration.LeftShankLength = double.Parse(child.Attributes["LShank"].Value);
                    s.Calibration.LeftThighLength = double.Parse(child.Attributes["LThigh"].Value);
                    s.Calibration.RightShankLength = double.Parse(child.Attributes["RShank"].Value);
                    s.Calibration.RightThighLength = double.Parse(child.Attributes["RThigh"].Value);
                }
            }
            return s;
        }

        /// <summary>
        /// Saves calibration information into session XML file.
        /// </summary>
        /// <param name="node">Parent session node</param>
        /// <param name="c">Calibration object that holds all calibration information to be stored</param>
        public void SaveCalibrationData(XmlNode node, CalibrationModel c)
        {
            XmlNode oldCalibration = node.SelectSingleNode("Calibration");

            if(oldCalibration != null)
            {
                oldCalibration.ParentNode.RemoveChild(oldCalibration);
            }

            XmlNode vNode = _xmlSessionDoc.CreateNode(XmlNodeType.Element, "Calibration", "");
            CreateCalibrationEntry(ref vNode, c);
            node.AppendChild(vNode);

            _xmlSessionDoc.Save(System.Configuration.ConfigurationManager.AppSettings["XmlSessionsFile"]);
        }

        /// <summary>
        /// Load Sessions.xml which contains all information about the registered sessions and related videos.
        /// It is called from the controller of starting page (ApplicationViewModel).
        /// </summary>
        public void LoadSessions()
        {
            if (!_loaded)
            {
                XmlSessionDoc = new XmlDocument();
                string xmlFilename = System.Configuration.ConfigurationManager.AppSettings["xmlSessionsFile"];

                if (!File.Exists(xmlFilename))
                {
                    XmlDeclaration xmlDeclaration = XmlSessionDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                    XmlElement  xNode = _xmlSessionDoc.CreateElement("Sessions");
                    XmlSessionDoc.AppendChild(xmlDeclaration);
                    XmlSessionDoc.AppendChild(xNode);
                    XmlSessionDoc.Save(xmlFilename);
                }
                else
                {
                    XmlSessionDoc.Load(xmlFilename);
                }

                XmlNodeList nodeList = XmlSessionDoc.DocumentElement.SelectNodes("/Sessions/Session");

                foreach (XmlNode node in nodeList)
                {
                    _allSessions.Add(LoadSession(node));
                }
                _loaded = true;
            }
        }

        /// <summary>
        /// Auxiliary method to add an attribute to a Xml Node.
        /// </summary>
        /// <param name="xNode">The which will receive the attribute</param>
        /// <param name="attName">Attribute´s name</param>
        /// <param name="attValue">Attribute´s value</param>
        private void CreateAttribute(XmlNode xNode, string attName, string attValue)
        {
            XmlAttribute xAtt = _xmlSessionDoc.CreateAttribute(attName);
            xAtt.Value = attValue;
            xNode.Attributes.Append(xAtt);
        }

        /// <summary>
        /// Save new or edited session entry, as well as remove it from XML Sessions file.
        /// </summary>
        private void SaveSession()
        {
            XmlNode xNode = null;

            if (!_loaded) LoadSessions();

            if (Operation == "Edit" || Operation == "Remove")
            {
                string xpath = "/Sessions/Session[@Id='{0}']";
                xpath = String.Format(xpath, _session.SessionId);
                xNode = _xmlSessionDoc.DocumentElement.SelectSingleNode(xpath);

                // Remove all attributes and children.
                if (xNode != null)
                {
                    xNode.ParentNode.RemoveChild(xNode);
                }
            }

            if (Operation != "Remove")
            {
                xNode = _xmlSessionDoc.CreateNode(XmlNodeType.Element, "Session", "");

                CreateAttribute(xNode, "Id", _session.SessionId.ToString());
                CreateAttribute(xNode, "Name", _session.SessionName);
                CreateAttribute(xNode, "Thigh", _session.ThighLength.ToString("0.000"));
                CreateAttribute(xNode, "Shank", _session.ShankLength.ToString("0.000"));
                CreateAttribute(xNode, "Date", _session.SessionDate);
                CreateAttribute(xNode, "Modality", _session.Modality);
                CreateAttribute(xNode, "Type", _session.SessionType);

                int sequence = 1;
                string calibration = "False";
                foreach (VideoModel video in _session.VideoList)
                {
                    XmlNode vNode = _xmlSessionDoc.CreateNode(XmlNodeType.Element, "Video", "");
                    CreateAttribute(vNode, "Sequence", sequence.ToString());
                    CreateAttribute(vNode, "Power", video.Power.ToString());

                    if(video.IsCalibration)
                    {
                        calibration = "True";

                    }
                    else
                    {
                        calibration = "False";
                    }

                    CreateAttribute(vNode, "Calibration", calibration);

                    vNode.InnerText = video.Filename;
                    xNode.AppendChild(vNode);
                    sequence++;
                }

                if(_session.Calibration != null && _session.Calibration.CalSessionId > 0)
                {
                    XmlNode cNode = _xmlSessionDoc.CreateNode(XmlNodeType.Element, "Calibration", "");

                    CreateCalibrationEntry(ref cNode, _session.Calibration);
                    xNode.AppendChild(cNode);
                }

                if (Operation == "Create" || Operation == "Edit") _xmlSessionDoc.LastChild.AppendChild(xNode);
            }

            _xmlSessionDoc.Save(System.Configuration.ConfigurationManager.AppSettings["XmlSessionsFile"]);
            
            // Sort the file if a new item was created.
            // We open the file again because we are using XDocument format instead of XMLDocument as above.
            if (Operation == "Create" || Operation == "Edit")
            {
                var xDoc = XDocument.Load(System.Configuration.ConfigurationManager.AppSettings["XmlSessionsFile"]);
                var newxDoc = new XElement("Sessions", xDoc.Root
                    .Elements()
                    .OrderBy(x => (int)x.Attribute("Id")));

                newxDoc.Save(System.Configuration.ConfigurationManager.AppSettings["XmlSessionsFile"]);
            }

            _appViewModel.CurrentPageViewModel = _previousPage;
        }

        private void CreateCalibrationEntry(ref XmlNode node, CalibrationModel c)
        {
            int j = (int)c.JointType;
            CreateAttribute(node, "JointType", j.ToString());

            CreateAttribute(node, "NumFrames", c.NumFrames.ToString());
            CreateAttribute(node, "InitialTime", c.InitialTime.ToString());

            CreateAttribute(node, "X", c.Position.X.ToString("0.00000"));
            CreateAttribute(node, "Y", c.Position.Y.ToString("0.00000"));
            CreateAttribute(node, "Z", c.Position.Z.ToString("0.00000"));

            CreateAttribute(node, "TX", c.Threshold.X.ToString("0.00000"));
            CreateAttribute(node, "TY", c.Threshold.Y.ToString("0.00000"));
            CreateAttribute(node, "TZ", c.Threshold.Z.ToString("0.00000"));

            CreateAttribute(node, "SDX", c.SD.X.ToString("0.00000"));
            CreateAttribute(node, "SDY", c.SD.Y.ToString("0.00000"));
            CreateAttribute(node, "SDZ", c.SD.Z.ToString("0.00000"));

            CreateAttribute(node, "EX", c.Estimated.X.ToString("0.00000"));
            CreateAttribute(node, "EY", c.Estimated.Y.ToString("0.00000"));
            CreateAttribute(node, "EZ", c.Estimated.Z.ToString("0.00000"));

            CreateAttribute(node, "LShank", c.LeftShankLength.ToString("0.000"));
            CreateAttribute(node, "LThigh", c.LeftThighLength.ToString("0.000"));
            CreateAttribute(node, "RShank", c.RightShankLength.ToString("0.000"));
            CreateAttribute(node, "RThigh", c.RightThighLength.ToString("0.000"));
        }

        /// <summary>
        /// Ignore changes and return to previous page.
        /// </summary>
        private void CancelSession()
        {
            // Return to previous page.
            _appViewModel.CurrentPageViewModel = _previousPage;
        }
    }
}
