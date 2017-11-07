using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sessions
{
    /// <summary>
    /// Author: Antonio Iyda Paganelli
    /// 
    /// Interaction logic for SessionsView.xaml
    /// </summary>
    public partial class SessionsView : UserControl
    {
        public SessionsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Deletes the selected video from VideoList.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDeleteVideo_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            var list = button.FindName("NewVideoList") as ListBox;
            SessionViewModel session = (SessionViewModel)this.DataContext;

            if (list.SelectedItems.Count > 0)
            {
                session.VideoList.RemoveAt(list.SelectedIndex);
            }
        }

        /// <summary>
        /// Adds a new video to VideoList.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAddVideo_Click(object sender, RoutedEventArgs e)
        {
            VideoModel vm = new VideoModel();
            SessionViewModel session = (SessionViewModel)this.DataContext;
            VideoInputDialog input = new VideoInputDialog();
            input.DataContext = vm;

            if (input.ShowDialog() == true)
            {
                vm = (VideoModel)input.DataContext;

                if (vm.Filename.Length > 0 && vm.Power > 0)
                {
                    session.VideoList.Add(vm);
                }
            }
        }

        /// <summary>
        /// Hidden UIelement carrying SessionId. It is used to synchronize data between XmlDataProvider 
        /// and object data context in order to call Edit and Delete commands on the right session record. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbId_TextChanged(object sender, TextChangedEventArgs e)
        {
            SessionsViewModel session = (SessionsViewModel)this.DataContext;
            TextBox txt = (TextBox)sender;
            session.SessionId = Int32.Parse(txt.Text);
        }

        private void btSave_Loaded(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            if (button.Content.ToString() != "Save")
            {
                button.Background = Brushes.Red;
            }
        }
    }
}
