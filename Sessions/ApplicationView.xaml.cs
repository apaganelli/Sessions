using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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
using System.Xml;
using System.Xml.Linq;

namespace Sessions
{
    /// <summary>
    /// Author: Antonio Iyda Paganelli
    /// 
    /// Interaction logic for ApplicationView.xaml
    /// It manages the interaction with all other pages (User Control) activating them when they are selected.
    /// Instantiate the related view models the first time a view is selected.
    /// The Application View Model object handles the interaction with this View.
    /// </summary>
    public partial class ApplicationView : Window
    {
        private SessionsViewModel sessionsViewModel = null;
        private WelcomeViewModel welcomeViewModel = null;
        private RecordViewModel recordViewModel = null;
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

                if(TabItemWelcome.IsSelected)
                {
                    if(welcomeViewModel == null)
                    {
                        welcomeViewModel = new WelcomeViewModel(app);
                    }
                    app.CurrentPageViewModel = welcomeViewModel;

                } else if(TabItemRecord.IsSelected)
                {
                    if(recordViewModel == null)
                    {
                        recordViewModel = new RecordViewModel(app);
                    }

                    app.CurrentPageViewModel = recordViewModel;

                } else if (TabItemCalibration.IsSelected)
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

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Check if it is the first time the application is loaded. If it is the case,
            // configure the directory to store the data.
            if(System.Configuration.ConfigurationManager.AppSettings["FirstTime"] == "T")
            {
                string filename = "";
                System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
                System.Windows.Forms.DialogResult result = dlg.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    filename = dlg.SelectedPath;
                    filename += "\\BioTechSessions.xml";

                    if (!File.Exists(filename))
                    {
                        XDocument doc = new XDocument(new XElement("Sessions"));
                        doc.Save(filename);
                    }

                    string appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string configFile = System.IO.Path.Combine(appPath, "Sessions.exe.config");
                    ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
                    configFileMap.ExeConfigFilename = configFile;
                    System.Configuration.Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);

                    config.AppSettings.Settings["FirstTime"].Value = "F";
                    config.AppSettings.Settings["xmlSessionsFile"].Value = filename;
                    config.Save();

                    MessageBox.Show("The directory was configured. Please, restart de application.");
                    System.Windows.Application.Current.Shutdown();
                }

            }
        }
    }
}
