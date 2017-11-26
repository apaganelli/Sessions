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
    /// Author: Antonio Iyda Paganelli
    /// 
    /// Interaction logic for ResultsView.xaml
    /// </summary>
    public partial class ResultsView : UserControl
    {
        public ResultsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            int num = 0;

            if (int.TryParse(tbPartitions.Text, out num))
            {
                this.tbFirstSet.IsEnabled = num > 0 ? true : false;
                this.tbSecondSet.IsEnabled = num > 0 ? true : false;
            }

            if (int.TryParse(tbPartitions.Text, out num) && num == 0)
                this.tbPartitions.Text = "";

            if (int.TryParse(tbFirstSet.Text, out num) && num == 0)
                this.tbFirstSet.Text = "";

            if (int.TryParse(tbSecondSet.Text, out num) && num == 0)
                this.tbSecondSet.Text = "";
        }

        private void tbPartitions_TextChanged(object sender, TextChangedEventArgs e)
        {
            int num = 0;

            if(int.TryParse(tbPartitions.Text, out num))
            {
                tbFirstSet.IsEnabled = num > 0 ? true : false;

                if(num > 0)
                {
                    int total = 0;

                    if(int.TryParse(tbTotalFrames.Text, out total))
                    {
                        tbPartitionSize.Text = Math.Abs(total / num).ToString();
                    }
                }
            }
        }

        private void tbFirstSet_TextChanged(object sender, TextChangedEventArgs e)
        {
            int numPartition = 0;
            int numFirst = 0;

            if (int.TryParse(tbPartitions.Text, out numPartition) && int.TryParse(tbFirstSet.Text, out numFirst))
            {
                tbSecondSet.IsEnabled = numPartition > 0 && numFirst > 0 && numFirst < numPartition ? true : false;
            }
        }
    }
}
