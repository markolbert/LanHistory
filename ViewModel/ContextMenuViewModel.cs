using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Windows;
using FileHistory;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Olbert.LanHistory.Model;
using Serilog;

namespace Olbert.LanHistory.ViewModel
{
    public class ContextMenuViewModel : ViewModelBase
    {
        public class DefaultBackupInterval : ViewModelBase
        {
            private TimeSpan _interval;
            private bool _isSelected;

            public DefaultBackupInterval( RelayCommand<TimeSpan> setBackupIntervalCommand )
            {
                SetBackupIntervalCommand = setBackupIntervalCommand;
            }

            public RelayCommand<TimeSpan> SetBackupIntervalCommand { get; }

            public TimeSpan Interval
            {
                get => _interval;
                set => Set<TimeSpan>( ref _interval, value );
            }

            public bool IsSelected
            {
                get => _isSelected;
                set => Set<bool>( ref _isSelected, value );
            }
        }

        //private readonly LanHistoryModel _lhModel;
        private readonly ILogger _logger;
        private bool _backupSucceeded = true;
        private bool _canBackup;
        private bool _canWakeServer;
        private DateTime _lastBackup;
        private bool _backupTimerRunning;
        private TimeSpan _interval;
        private TimeSpan _timeRemaining;

        private List<DefaultBackupInterval> _defIntervals =
            new List<DefaultBackupInterval>();

        public ContextMenuViewModel()
        {
            var vml = new ViewModelLocator();
            _logger = vml.Logger;
            LastBackup = vml.Configuration.LastBackup;


            using( var lhm = vml.LanHistoryModel )
            {
                Interval = lhm.Interval;
                CanBackup = lhm.IsValid;
                CanWakeServer = lhm.MacAddressIsValid;
            }

            using( var bt = vml.BackupTimer )
            {
                BackupTimerRunning = bt.Enabled;
                TimeRemaining = bt.TimeRemaining;
            }

            ExitApplicationCommand = new RelayCommand( () => Application.Current.Shutdown() );

            ShowConfigurationWindowCommand =
                new RelayCommand( () => ShowHideMainWindow( true ), () => !IsMainWindowOpen() );

            HideConfigurationWindowCommand =
                new RelayCommand( () => ShowHideMainWindow( false ), () => !IsMainWindowOpen() );

            BackupCommand = new RelayCommand(Backup, ()=>CanBackup);
            WakeServerCommand = new RelayCommand( SendWakeOnLan, ()=> CanWakeServer );
            SetBackupIntervalCommand = new RelayCommand<TimeSpan>(SetBackupInterval);

            Messenger.Default.Register<BackupResultMessage>( this, BackupResultMessageHandler );
            Messenger.Default.Register<BackupTimerTickMessage>(this, BackupTimerTickMessageHandler);
            Messenger.Default.Register<EnableTimerMessage>( this, EnableTimerMessageHandler );

            foreach( var interval in new int[] { 5, 10, 15, 30, 60, 120, 720, 1440 } )
            {
                DefaultBackupInterval dbi = new DefaultBackupInterval(SetBackupIntervalCommand) { Interval = TimeSpan.FromMinutes( interval ) };
                dbi.IsSelected = dbi.Interval.Equals( Interval );

                _defIntervals.Add( dbi );
            }
        }

        public bool CanBackup
        {
            get => _canBackup;

            set
            {
                Set<bool>( ref _canBackup, value );

                RaisePropertyChanged( () => StatusMesg );
            }
        }

        public bool CanWakeServer
        {
            get => _canWakeServer;

            set
            {
                Set<bool>( ref _canWakeServer, value );

                RaisePropertyChanged( () => NextBackup );
            }
        }

        public bool BackupTimerRunning
        {
            get => _backupTimerRunning;

            set
            {
                Set<bool>( ref _backupTimerRunning, value );

                RaisePropertyChanged( () => NextBackup );
            }
        }

        public TimeSpan Interval
        {
            get => _interval;
            set
            {
                Set<TimeSpan>( ref _interval, value );

                using( var lh = new ViewModelLocator().LanHistoryModel )
                {
                    lh.Interval = value;
                }

                foreach( var defInterval in DefaultIntervals )
                {
                    defInterval.IsSelected = defInterval.Interval.Equals( value );
                }

                RaisePropertyChanged( () => DefaultIntervals );
            }
        }

        public List<DefaultBackupInterval> DefaultIntervals
        {
            get => _defIntervals;
            set => Set<List<DefaultBackupInterval>>( ref _defIntervals, value );
        }

        public TimeSpan TimeRemaining
        {
            get => _timeRemaining;

            set
            {
                Set<TimeSpan>( ref _timeRemaining, value );

                RaisePropertyChanged( () => NextBackup );
            }
        }

        public bool BackupSucceeded
        {
            get => _backupSucceeded;

            set
            {
                Set<bool>( ref _backupSucceeded, value );

                RaisePropertyChanged( () => StatusMesg );
            }
        }

        public DateTime LastBackup
        {
            get => _lastBackup;

            set
            {
                Set<DateTime>( ref _lastBackup, value );

                RaisePropertyChanged( () => StatusMesg );
            }
        }

        public string StatusMesg
        {
            get
            {
                if( CanBackup )
                {
                    if( !BackupSucceeded ) return "Manual backup failed";

                    return $"Last: {_lastBackup: M/d/yyyy h:mm tt}";

                }

                return "Configuration required";
            }
        }

        public string NextBackup
        {
            get
            {
                if( CanBackup && BackupTimerRunning )
                    return "Next: " + TimeRemaining.ToString( "h':'mm" );

                return "Next: not running";
            }
        }

        public RelayCommand ExitApplicationCommand { get; }
        public RelayCommand ShowConfigurationWindowCommand { get; }
        public RelayCommand HideConfigurationWindowCommand { get; }
        public RelayCommand BackupCommand { get; }
        public RelayCommand WakeServerCommand { get; }
        public RelayCommand<TimeSpan> SetBackupIntervalCommand { get; }

        private void Backup()
        {
            try
            {
                using (FileHistoryService fhs = new FileHistoryService())
                {
                    fhs.Start();
                }

                _logger.Information("Started backup");
                _backupSucceeded = true;
            }
            catch (Exception e)
            {
                _backupSucceeded = true;
                _logger.Error(e, "Backup failed; message was {Message}");

                RaisePropertyChanged( () => StatusMesg );
            }
        }

        private void ShowHideMainWindow(bool show)
        {
            Application.Current.MainWindow = new MainWindow();

            if( show ) Application.Current.MainWindow.Show();
            else Application.Current.MainWindow.Hide();
        }

        private bool IsMainWindowOpen()
        {
            return Application.Current.MainWindow != null;
        }

        private void SendWakeOnLan()
        {
            using( var lhModel = new ViewModelLocator().LanHistoryModel )
            {
                (bool succeeded, string mesg) = lhModel.SendWakeOnLan();

                if (succeeded) _logger.Information(mesg);
                else _logger.Error(mesg);
            }
        }

        private void SetBackupInterval( TimeSpan timeSpan )
        {
            Interval = timeSpan;

            ConfigurationViewModel cvm = new ViewModelLocator().Configuration;

            Messenger.Default.Send<ConfigurationChangedMessage>(
                new ConfigurationChangedMessage()
                {
                    Interval = Interval,
                    MacAddress = cvm.MacAddress,
                    WakeUpTime = cvm.WakeUpTime
                } );
        }

        private void BackupResultMessageHandler(BackupResultMessage obj)
        {
            BackupSucceeded = obj?.Succeeded ?? false;
        }

        private void BackupTimerTickMessageHandler(BackupTimerTickMessage obj)
        {
            if( obj != null ) TimeRemaining = obj.TimeRemaining;
        }

        private void EnableTimerMessageHandler( EnableTimerMessage obj )
        {
            BackupTimerRunning = obj?.Enabled ?? false;
        }

    }
}
