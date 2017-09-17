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
