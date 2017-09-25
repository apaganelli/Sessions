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
    /// SessionModelView is used to edit or create a new Session SessionModel. Actually, it is not a view, but
    /// another controller to SessionsView.
    /// </summary>
    class SessionViewModel: ObservableObject, IPageViewModel
    {
        private ApplicationViewModel _appViewModel;
        private SessionModel _session;
        private IPageViewModel _previousPage;

        private ICommand _saveSessionCommand;
        private ICommand _cancelSessionCommand;

        private bool _loaded = false;
        private List<SessionModel> _allSessions = new List<SessionModel>();
        private XmlDocument _xmlSessionDoc = null;
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

        public string ButtonText
        {
            get { return _buttonText; }
        }

        // IPageViewModel required property.
        public string Name
        {
            get { return "Session View"; }
        }

        public int SessionId
        {
            get { return _session.SessionId; }
            set
            {
                if (value != _session.SessionId) _session.SessionId = value;
            }
        }

        public string SessionName
        {
            get { return _session.SessionName; }
            set
            {
                if (value != _session.SessionName) _session.SessionName = value;
            }
        }

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

        public CalibrationModel Calibration
        {
            get { return _session.Calibration; }
        }

        public SessionModel Session
        {
            get { return _session; }
        }

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

                    float x, y, z;
                    x = float.Parse(child.Attributes["X"].Value);
                    y = float.Parse(child.Attributes["Y"].Value);
                    z = float.Parse(child.Attributes["Z"].Value);

                    s.Calibration.Position = new Vector3(x, y, z);
                }
            }

            return s;
        }

        public void SaveCalibrationData(XmlNode node, CalibrationModel c)
        {
            XmlNode oldCalibration = node.SelectSingleNode("Calibration");

            if(oldCalibration != null)
            {
                oldCalibration.ParentNode.RemoveChild(oldCalibration);
            }

            XmlNode vNode = _xmlSessionDoc.CreateNode(XmlNodeType.Element, "Calibration", "");
            int j = (int) c.JointType;
            CreateAttribute(vNode, "JointType", j.ToString());
            CreateAttribute(vNode, "NumFrames", c.NumFrames.ToString());
            CreateAttribute(vNode, "X", c.Position.X.ToString("0.00000"));
            CreateAttribute(vNode, "Y", c.Position.Y.ToString("0.00000"));
            CreateAttribute(vNode, "Z", c.Position.Z.ToString("0.00000"));
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

                    CreateAttribute(cNode, "NumFrames", _session.Calibration.NumFrames.ToString());
                    int j = (int)_session.Calibration.JointType;
                    CreateAttribute(cNode, "JointType", j.ToString());
                    CreateAttribute(cNode, "X", _session.Calibration.Position.X.ToString());
                    CreateAttribute(cNode, "Y", _session.Calibration.Position.Y.ToString());
                    CreateAttribute(cNode, "Z", _session.Calibration.Position.Z.ToString());
                    xNode.AppendChild(cNode);
                }

                if (Operation == "Create" || Operation == "Edit") _xmlSessionDoc.LastChild.AppendChild(xNode);
            }

            _xmlSessionDoc.Save(System.Configuration.ConfigurationManager.AppSettings["XmlSessionsFile"]);

            // Sort the file if a new item was created.
            if (Operation == "Create")
            {
                var xDoc = XDocument.Load(System.Configuration.ConfigurationManager.AppSettings["XmlSessionsFile"]);
                var newxDoc = new XElement("Sessions", xDoc.Root
                    .Elements()
                    .OrderBy(x => (int)x.Attribute("Id")));

                newxDoc.Save(System.Configuration.ConfigurationManager.AppSettings["XmlSessionsFile"]);
            }

            _appViewModel.CurrentPageViewModel = _previousPage;
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
