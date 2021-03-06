﻿using System;
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
        private CalibrationViewModel calViewModel = null;
        private AnalysisViewModel exeViewModel = null;
        private ResultsViewModel resultViewModel = null;

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
                    if (calViewModel == null)
                    {
                        calViewModel = new CalibrationViewModel(app);
                    }

                    calViewModel.LoadCalibrationData(false);
                    app.CurrentPageViewModel = calViewModel;
                }
                else if (TabItemExecution.IsSelected)
                {
                    if (exeViewModel == null)
                    {
                        exeViewModel = new AnalysisViewModel(app);
                    }

                    exeViewModel.LoadExecutionModel();
                    app.CurrentPageViewModel = exeViewModel;
                } else if (TabItemResults.IsSelected)
                {
                    if(resultViewModel == null)
                    {
                        resultViewModel = new ResultsViewModel(app);
                    }

                    resultViewModel.LoadResultViewModel();
                    app.CurrentPageViewModel = resultViewModel;
                } else if(TabItemSessions.IsSelected)
                {
                    // This is the first and default page.
                    app.CurrentPageViewModel = app.PageViewModels[0];
                }
            }
        }
    }
}
