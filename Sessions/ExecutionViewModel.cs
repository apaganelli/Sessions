using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        #region Constructors

        /// <summary>
        /// Controls exercise test process online or offline.
        /// </summary>
        /// <param name="app">Pointer to application controller and interface to access other modules</param>
        public ExecutionViewModel(ApplicationViewModel app)
        {
            _app = app;
        }

        #endregion // Constructors

        /// <summary>
        /// Identifies PageViewModel.
        /// </summary>
        public string Name
        {
            get { return "Execution View"; }
        }

    }
}
