using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Sessions
{
    /// <summary>
    /// Author: Antonio Iyda Paganelli
    /// 
    /// It instantiates the ApplicationView (window and shows it) and the ApplicationViewModel linking it as the view
    /// DataContext which will handles all user interface events.
    /// 
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ApplicationView app = new ApplicationView();
            ApplicationViewModel _appContext = new ApplicationViewModel();
            app.DataContext = _appContext;
            app.Show();
        }
    }



}
