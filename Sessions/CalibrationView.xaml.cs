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
        public CalibrationView()
        {
            InitializeComponent();
        }

        private void lbJointNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalibrationViewModel context = (CalibrationViewModel)DataContext;
            context.SelectedJoint = lbJointNames.SelectedValue.ToString();
        }

        private void txCalStatus_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalibrationViewModel context = (CalibrationViewModel)DataContext;

            if(context.ProcessedFrames >= context.NumFrames)
            {
                TextBox bt =  (TextBox)FindName("txNumFrames");
                bt.Focus();
            }
        }
    }
}
