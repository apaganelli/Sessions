using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Sessions
{
    /// <summary>
    /// Interaction logic for ApplicationView.xaml
    /// </summary>
    public partial class ApplicationView : Window
    {
        private bool firstCalibrationPage = true;
        private bool firstExecutionPage = true;

        private CalibrationViewModel calViewModel = null;
        private ExecutionViewModel exeViewModel = null;

        public ApplicationView()
        {
            InitializeComponent();

        }


        /// <summary>
        /// Selects the user control to show within the tab area. Creates a new ViewModel, if the case and
        /// pass it to ApplicationViewModel (shell that controls navigation).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.Source is TabControl)
            {
                ApplicationViewModel app = (ApplicationViewModel)DataContext;

                if (TabItemCalibration.IsSelected)
                {
                    if (firstCalibrationPage)
                    {
                        calViewModel = new CalibrationViewModel(app);
                        firstCalibrationPage = false;
                    }

                    calViewModel.LoadCalibrationData(false);
                    app.CurrentPageViewModel = calViewModel;
                } else if(TabItemExecution.IsSelected)
                { 
                    if(firstExecutionPage)
                    {
                        exeViewModel = new ExecutionViewModel(app);
                        firstExecutionPage = false;
                    }

                    exeViewModel.LoadExecutionModel();
                    app.CurrentPageViewModel = exeViewModel;
                } else if(TabItemSessions.IsSelected)
                {
                    // This is the first and default page.
                    app.CurrentPageViewModel = app.PageViewModels[0];
                }
            }
        }
    }
}
