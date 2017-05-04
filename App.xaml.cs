using System.Windows;
using GalaSoft.MvvmLight.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Olbert.LanHistory.ViewModel;

namespace Olbert.LanHistory
{
    public partial class App : Application
    {
        private TaskbarIcon _notifyIcon;
        private BackupTimer _timer;

        static App()
        {
            DispatcherHelper.Initialize();
        }

        protected override void OnStartup( StartupEventArgs e )
        {
            base.OnStartup( e );

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            _notifyIcon = (TaskbarIcon) FindResource( "NotifyIcon" );

            ViewModelLocator locator = (ViewModelLocator) FindResource( "Locator" );
            _timer = locator?.BackupTimer;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            //the icon would clean up automatically, but this is cleaner
            _notifyIcon?.Dispose(); 
            _timer?.Dispose();

            ViewModelLocator locator = (ViewModelLocator)FindResource("Locator");
            locator?.LanHistory.Save();

            base.OnExit(e);
        }
    }
}
