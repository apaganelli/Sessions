using Microsoft.Win32;
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
    /// Interaction logic for VideoInputDialog.xaml
    /// </summary>
    public partial class VideoInputDialog : Window
    {
        public VideoInputDialog()
        {
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            btFile.Focus();
            txtAnswer.SelectAll();
        }

        private void File_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Cursor Files|*.xef";
            openFileDialog.Title = "Select Video files";

            if (openFileDialog.ShowDialog() == true)
            {
                lbFilename.Content = openFileDialog.FileName;
            }
        }

        private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            VideoModel video = (VideoModel)DataContext;
            video.Filename = lbFilename.Content.ToString();
            video.Power = Int32.Parse(txtAnswer.Text);
            video.IsCalibration = (bool) chkCalibration.IsChecked;
            this.DialogResult = true;
        }            
    }
}
