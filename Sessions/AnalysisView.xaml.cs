using System.Windows;
using System.Windows.Controls;

namespace Sessions
{
    /// <summary>
    /// Interaction logic for AnalysisView.xaml
    /// </summary>
    public partial class AnalysisView : UserControl
    {
        // Holds the data context of the view.
        private AnalysisViewModel context;

        public AnalysisView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Gets the current data context of the view
            context = (AnalysisViewModel)DataContext;

            // Updates view model properties with current context.
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

            // Sets pointers to canvas objects on this User Control in order to update theirs contents.
            context.CanvasSkeleton = CanvasPosition;
            context.CanvasImage = CanvasImage;
            context.FilteredCanvas = FilteredCanvas;

            // The data context joint positions is based on another class that is only instantiated
            // after the creation of this User Control. Then, it is necessary update UI objects with this data context.
            KinectBodyView kbv = context.GetKinectBodyView();
            this.kinectBodyViewbox.DataContext = kbv;
            this.txtHipLeftX.DataContext = kbv;
            this.txtHipRightX.DataContext = kbv;
            this.txtHipLeftY.DataContext = kbv;
            this.txtHipRightY.DataContext = kbv;
            this.txtHipLeftZ.DataContext = kbv;
            this.txtHipRightZ.DataContext = kbv;

            this.txtKneeLeftX.DataContext = kbv;
            this.txtKneeRightX.DataContext = kbv;
            this.txtKneeLeftY.DataContext = kbv;
            this.txtKneeRightY.DataContext = kbv;
            this.txtKneeLeftZ.DataContext = kbv;
            this.txtKneeRightZ.DataContext = kbv;

            this.txtAnkleLeftX.DataContext = kbv;
            this.txtAnkleRightX.DataContext = kbv;
            this.txtAnkleLeftY.DataContext = kbv;
            this.txtAnkleRightY.DataContext = kbv;
            this.txtAnkleLeftZ.DataContext = kbv;
            this.txtAnkleRightZ.DataContext = kbv;

            this.txtLeftShankLen.DataContext = kbv;
            this.txtLeftThighLen.DataContext = kbv;
            this.txtRightShankLen.DataContext = kbv;
            this.txtRightThighLen.DataContext = kbv;
        }
    }
}
