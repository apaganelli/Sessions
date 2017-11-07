using System.Windows;
using System.Windows.Controls;

namespace Sessions
{
    /// <summary>
    /// Author: Antonio Iyda Paganelli
    /// Interaction logic for RecordView.xaml
    /// </summary>
    public partial class RecordView : UserControl
    {
        private RecordViewModel context = null;

        public RecordView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Loads view object references for ViewBoxes of the UserControl.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //RecordView data context is a RecordViewModel instance.
            context = (RecordViewModel) this.DataContext;

            // set data context for display in UI
            this.TextElapsedTime.DataContext = context.GetKinectBodyView;
            this.TextMissingFrames.DataContext = context.GetKinectBodyView;

            this.kinectIRViewbox.DataContext = context.GetKinectIRView;
            this.kinectDepthViewbox.DataContext = context.GetKinectDepthView;
            this.kinectBodyIndexViewbox.DataContext = context.GetKinectBodyIndexView;
            this.kinectBodyViewbox.DataContext = context.GetKinectBodyView;
        }
    }
}
