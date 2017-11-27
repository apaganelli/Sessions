using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;

namespace Sessions
{
    /// <summary>
    /// Author: Antonio Iyda Paganelli
    /// 
    /// Controls user interface events and all logic for Session processes.
    /// </summary>
    class SessionsViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        private int _sessionId;
        private int _selectedSessionId = 0; 
        private SessionModel _currentSession;
        private string _status;

        // Pointer to the controller of this object. Used to inform to change pages.
        private ApplicationViewModel _applicationViewModel;


        // Actions on the UserObject. (new, edit, delete and select the current session).
        private ICommand _newSessionCommand; 
        private ICommand _editSessionCommand;
        private ICommand _deleteSessionCommand;
        private ICommand _selectSessionCommand;

        #endregion Fields


        /// <summary>
        /// Primary Constructor
        /// </summary>
        /// <param name="appViewModel">Reference to main controller. ApplicationViewModel</param>
        public SessionsViewModel(ApplicationViewModel appViewModel)
        {
            _applicationViewModel = appViewModel;
        }

        #region Properties
        /// <summary>
        /// Gets/sets selected session ID.
        /// </summary>
        public int SelectedSessionId
        {
            get { return _selectedSessionId; }
            set {
                if (value != _selectedSessionId)
                {
                    _selectedSessionId = value;
                    OnPropertyChanged("SelectedSessionId");
                }
            }
        }
       
        /// <summary>
        /// Gets/sets current object class.
        /// </summary>
        public SessionModel CurrentSession
        {
            get { return _currentSession; }
            set
            {
                if (value != _currentSession)
                {
                    _currentSession = value;
                    OnPropertyChanged("CurrentSession");
                }
            }
        }

        /// <summary>
        /// Shows status messages.
        /// </summary>
        public String Status
        {
            get { return _status; }
            set
            {
                if(_status != value)
                {
                    _status = value;
                    OnPropertyChanged("Status");
                }
            }
        }


        public ICommand NewSessionCommand
        {
            get
            {
                if(_newSessionCommand == null)
                {
                    _newSessionCommand = new RelayCommand(param => NewSession());
                }

                return _newSessionCommand;
            }
        }

        public ICommand EditSessionCommand
        {
            get
            {
                if (_editSessionCommand == null)
                {
                    _editSessionCommand = new RelayCommand(param => EditSession(), param => (SessionId > 0));
                }
                return _editSessionCommand;
            }
        }

        public ICommand DeleteSessionCommand
        {
            get
            {
                if (_deleteSessionCommand == null)
                {
                    _deleteSessionCommand = new RelayCommand(param => DeleteSession(), param => (SessionId > 0));
                }
                return _deleteSessionCommand;
            }
        }

        public ICommand SelectSessionCommand
        {
            get
            {
                if(_selectSessionCommand == null)
                {
                    _selectSessionCommand = new RelayCommand(param => SelectSession(), param => (SessionId > 0));
                }
                return _selectSessionCommand;
            }
        }


        public int SessionId
        {
            get { return _sessionId; }
            set
            {
                if (_sessionId != value)
                {
                    _sessionId = value;
                    OnPropertyChanged("SessionId");
                }
            }
        }

        /// <summary>
        /// Name that identifies the IPageViewModel 
        /// </summary>
        public string Name
        {
            get { return "Sessions View"; }
        }
        #endregion


        #region Commands
        /// <summary>
        /// Enter a new XML session record.
        /// </summary>
        private void NewSession()
        {
            // Need to load file to check which is the last SessionId
            _applicationViewModel.CurrentPageViewModel = new SessionViewModel(_applicationViewModel);
        }

        /// <summary>
        /// Edit an exiting session record.
        /// </summary>
        private void EditSession()
        {
            _applicationViewModel.CurrentPageViewModel = new SessionViewModel(_applicationViewModel, SessionId, false);
        }

        private void DeleteSession()
        {
            _applicationViewModel.CurrentPageViewModel = new SessionViewModel(_applicationViewModel, SessionId, true);
        }

        private void SelectSession()
        {
            SelectedSessionId = SessionId;
            Status = "Session ID " + SessionId.ToString() + " selected.";            
        }
#endregion
    }
}

