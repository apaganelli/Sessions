using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sessions
{
    class WelcomeViewModel : IPageViewModel
    {
        private ApplicationViewModel _app;

        public WelcomeViewModel(ApplicationViewModel app)
        {
            _app = app;
        }

        public string Name => throw new NotImplementedException();
    }
}
