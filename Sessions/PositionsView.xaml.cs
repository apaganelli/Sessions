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
    /// Interaction logic for PositionsView.xaml
    /// </summary>
    public partial class PositionsView : UserControl
    {
        public PositionsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            AnalysisViewModel context = (AnalysisViewModel)DataContext; 
            context.LoadExecutionModel();

            // If video has not calibration, calibration joint was not set, then there is no reason to show
            // joint calibration information.
            if (!context.HasCalibration)
            {
                lbCalibrationJoint.Visibility = Visibility.Hidden;
                lbJointName.Visibility = Visibility.Hidden;
                txtCalibrationX.Visibility = Visibility.Hidden;
                txtCalibrationY.Visibility = Visibility.Hidden;
                txtCalibrationZ.Visibility = Visibility.Hidden;
            } else
            {
                lbCalibrationJoint.Visibility = Visibility.Visible;
                lbJointName.Visibility = Visibility.Visible;
                txtCalibrationX.Visibility = Visibility.Visible;
                txtCalibrationY.Visibility = Visibility.Visible;
                txtCalibrationZ.Visibility = Visibility.Visible;
            }

            context.CanvasSkeleton = CanvasPosition;
            context.CanvasImage = CanvasImage;
            context.FilteredCanvas = FilteredCanvas;
        }

        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            AnalysisViewModel context = (AnalysisViewModel)DataContext;

        }
    }
}
