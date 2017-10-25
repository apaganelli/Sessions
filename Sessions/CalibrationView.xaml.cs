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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sessions
{
    /// <summary>
    /// Interaction logic for CalibrationView.xaml
    /// </summary>
    public partial class CalibrationView : UserControl
    {
        // Data context of the this view.
        CalibrationViewModel context;

        public CalibrationView()
        {
            InitializeComponent();
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            context = (CalibrationViewModel)DataContext;

            // Show calibration status only if there are frames processed. The process has started.
            lbCalStatus.Visibility = context.ProcessedFrames > 0 ? Visibility.Visible : Visibility.Hidden;
            txCalStatus.Visibility = context.ProcessedFrames > 0 ? Visibility.Visible : Visibility.Hidden;
        }


        private void lbJointNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            context = (CalibrationViewModel)DataContext;

            // Gets the name of the selected joint.
            context.SelectedJoint = lbJointNames.SelectedValue.ToString();
        }

        private void txCalStatus_TextChanged(object sender, TextChangedEventArgs e)
        {
            context = (CalibrationViewModel)DataContext;

            // Checks when the process has finished.
            if (context.ProcessedFrames >= context.NumFrames)
            {
                TextBox tBox =  (TextBox)FindName("txNumFrames");
                tBox.Focus();
            }
        }

        private void btRun_Click(object sender, RoutedEventArgs e)
        {
            // If the the run button was clicked show status.
            lbCalStatus.Visibility = Visibility.Visible;
            txCalStatus.Visibility = Visibility.Visible;
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            // Once have saved the information, hide status.
            lbCalStatus.Visibility = Visibility.Hidden;
            txCalStatus.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// If any of the three threshold has changed, signalize the view model. Data should be saved.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            context = (CalibrationViewModel)DataContext;
            context.CalibrationChanged = true;
        }
    }
}
