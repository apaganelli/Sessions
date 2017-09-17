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
    class SessionsViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        private int _sessionId;
        private int _selectedSessionId = 0; 
        private SessionModel _currentSession;

        // Pointer to the controller of this object. Used to inform to change pages.
        private ApplicationViewModel _applicationViewModel;

        private ICommand _newSessionCommand; 
        private ICommand _editSessionCommand;
        private ICommand _deleteSessionCommand;
        private ICommand _selectSessionCommand;

        #endregion //Fields

        public SessionsViewModel(ApplicationViewModel appViewModel)
        {
            _applicationViewModel = appViewModel;
        }

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
            MessageBox.Show("Session ID: " + SessionId.ToString() + " Selected");
        }


    }
}

