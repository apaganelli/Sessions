using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Sessions
{
    /// <summary>
    /// Author: Antonio Iyda Paganelli
    /// 
    /// The main object of the application that manages the starting page and holds pointers to all
    /// pages that had been selected. It also holds and inform which is the current active page.
    /// 
    /// </summary>
    class ApplicationViewModel : ObservableObject
    {
        private ICommand _changePageCommand;

        private IPageViewModel _currentPageViewModel;
        private List<IPageViewModel> _pageViewModels;

        private SessionsViewModel _sessionsViewModel;           // List of sessions
        private WelcomeViewModel _welcomeViewModel;

        /// <summary>
        /// The constructor activate the application default page and selects it as the active page.
        /// </summary>
        public ApplicationViewModel()
        {
            // Navigation pages
            // Sessions
            _sessionsViewModel = new SessionsViewModel(this);

            // Add all navigation pages.
            PageViewModels.Add(_sessionsViewModel);
 
            if (System.Configuration.ConfigurationManager.AppSettings["FirstTime"] == "T")
            {
                _welcomeViewModel = new WelcomeViewModel(this);
                PageViewModels.Add(_welcomeViewModel);
                CurrentPageViewModel = PageViewModels[1];
            } else
            {
                // Set up initial page.
                CurrentPageViewModel = PageViewModels[0];
            }
        }

        public SessionsViewModel SessionsViewModel
        {
            get { return _sessionsViewModel; }
        }

        /// <summary>
        /// Interface command to execute the change of pages.
        /// </summary>
        public ICommand ChangePageCommand
        {
            get
            {
                if (_changePageCommand == null)
                {
                    _changePageCommand = new RelayCommand(
                        p => ChangeViewModel((IPageViewModel)p),
                        p => p is IPageViewModel);
                }
                return _changePageCommand;
            }
        }

        /// <summary>
        /// Gets/sets the current view model page.
        /// </summary>
        public IPageViewModel CurrentPageViewModel
        {
            get
            {
                return _currentPageViewModel;
            }
            set
            {
                if (_currentPageViewModel != value)
                {
                    _currentPageViewModel = value;
                    OnPropertyChanged("CurrentPageViewModel");
                }
            }
        }

        /// <summary>
        /// Gets the list of all view model pages that had been instantiated.
        /// </summary>
        public List<IPageViewModel> PageViewModels
        {
            get
            {
                if (_pageViewModels == null)
                    _pageViewModels = new List<IPageViewModel>();

                return _pageViewModels;
            }
        }

        /// <summary>
        /// Changes the active selected view model. If it is a new view model that had not been activated before,
        /// adds it to the list of view model pages.
        /// </summary>
        /// <param name="viewModel"></param>
        private void ChangeViewModel(IPageViewModel viewModel)
        {
            if (!PageViewModels.Contains(viewModel))
                PageViewModels.Add(viewModel);

            CurrentPageViewModel = PageViewModels.FirstOrDefault(vm => vm == viewModel);
        }
    }
}
