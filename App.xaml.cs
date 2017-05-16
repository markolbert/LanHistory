using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using GalaSoft.MvvmLight.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Olbert.JumpForJoy.WPF;
using Olbert.LanHistory.ViewModel;

namespace Olbert.LanHistory
{
    public partial class App : Application
    {
        private static Mutex _mutex;
        private TaskbarIcon _notifyIcon;
        private BackupTimer _timer;

        static App()
        {
            DispatcherHelper.Initialize();
        }

        protected override void OnStartup( StartupEventArgs e )
        {
            // enforce singleton
            const string appName = "LanHistory";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if( !createdNew )
                Terminate( $"{appName} is already running, check the system tray..." );

            // make sure File History is configured and using a file share
            ViewModelLocator locator = (ViewModelLocator)FindResource("Locator");

            if( locator == null )
                Terminate("Could not create ViewModelLocator, terminating");

            var lh = locator.DataService.GetLanHistory();
            if( lh == null )
                Terminate("Could not find Windows File History information, terminating...");

            if( !lh.IsRemote )
                Terminate( "LanHistory only works when Windows File History is configured to use a network share, terminating..." );

            base.OnStartup( e );

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            _notifyIcon = (TaskbarIcon) FindResource( "NotifyIcon" );

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

        private void Terminate( string mesg )
        {
            new J4JMessageBox().Title( "LanHistory Message" ).Message( mesg ).ButtonText( "Okay" ).ShowMessageBox();
            Application.Current.Shutdown();
        }
    }
}
