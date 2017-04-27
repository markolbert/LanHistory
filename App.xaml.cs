using System.Timers;
using System.Windows;
using GalaSoft.MvvmLight.Threading;
using LanHistory.Model;
using LanHistory.Properties;
using Microsoft.Win32;

namespace LanHistory
{
    public partial class App : Application
    {
        static App()
        {
            DispatcherHelper.Initialize();
        }

        public App()
        {
        }

        protected override void OnStartup( StartupEventArgs e )
        {
            base.OnStartup( e );

            if( Settings.Default.Configuration == null )
                Settings.Default.Configuration = new FileHistoryInfo();
        }
    }
}
