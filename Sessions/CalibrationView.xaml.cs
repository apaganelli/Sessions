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
        CalibrationViewModel context;

        public CalibrationView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            context = (CalibrationViewModel)DataContext;
            lbCalStatus.Visibility = context.ProcessedFrames > 0 ? Visibility.Visible : Visibility.Hidden;
            txCalStatus.Visibility = context.ProcessedFrames > 0 ? Visibility.Visible : Visibility.Hidden;
        }


        private void lbJointNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            context = (CalibrationViewModel)DataContext;
            context.SelectedJoint = lbJointNames.SelectedValue.ToString();
        }

        private void txCalStatus_TextChanged(object sender, TextChangedEventArgs e)
        {
            context = (CalibrationViewModel)DataContext;
            if (context.ProcessedFrames >= context.NumFrames)
            {
                TextBox bt =  (TextBox)FindName("txNumFrames");
                bt.Focus();
            }
        }

        private void btRun_Click(object sender, RoutedEventArgs e)
        {
            lbCalStatus.Visibility = Visibility.Visible;
            txCalStatus.Visibility = Visibility.Visible;
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            lbCalStatus.Visibility = Visibility.Hidden;
            txCalStatus.Visibility = Visibility.Hidden;
        }


        /// <summary>
        /// Any threshold has changed
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
