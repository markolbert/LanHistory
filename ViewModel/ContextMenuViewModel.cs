using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Olbert.LanHistory.Model;

namespace Olbert.LanHistory.ViewModel
{
    public class ContextMenuViewModel : ValidatedViewModelBase
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

            public bool IsHitTestVisible { get; set; } = true;
        }

        private readonly IDataService _dataService;
        private bool _backupSucceeded = true;
        private bool? _shareAccessible;
        private readonly BackupTimer _backupTimer;
        private readonly Model.LanHistory _lanHistory;
        private List<DefaultBackupInterval> _defIntervals = new List<DefaultBackupInterval>();

        public ContextMenuViewModel()
        {
            var vml = new ViewModelLocator();
            _dataService = vml.DataService ?? throw new NullReferenceException( "IDataService" );
            _lanHistory = vml.LanHistory ?? throw new NullReferenceException( "LanHistory" );
            _backupTimer = vml.BackupTimer ?? throw new NullReferenceException( "BackupTimer" );

            OpeningEventCommand = new RelayCommand( OpeningEventHandler );
            ExitApplicationCommand = new RelayCommand( () => Application.Current.Shutdown() );

            BackupCommand = new RelayCommand( Backup, () => _lanHistory.IsValid && ( ShareAccessible ?? false ) );
            WakeServerCommand = new RelayCommand( SendWakeOnLan, () => _lanHistory.MacAddressIsValid );
            SetBackupIntervalCommand = new RelayCommand<TimeSpan>( SetBackupInterval );

            Messenger.Default.Register<BackupResultMessage>( this, BackupResultMessageHandler );
            Messenger.Default.Register<BackupTickMessage>( this, BackupTickMessageHandler );
            Messenger.Default.Register<EnableTimerMessage>( this, EnableTimerMessageHandler );
            Messenger.Default.Register<ServerStatusMessage>( this, ServerStatusMessageHandler );
            Messenger.Default.Register<LanHistoryMessage>( this, LanHistoryChangedMessageHandler );
            Messenger.Default.Register<PowerModeMessage>( this, PowerModeMessageHandler );

            foreach( var interval in new int[] { 5, 10, 15, 30, 60, 120, 720, 1440 } )
            {
                DefaultBackupInterval dbi =
                    new DefaultBackupInterval( SetBackupIntervalCommand )
                    {
                        Interval = TimeSpan.FromMinutes( interval )
                    };
                dbi.IsSelected = dbi.Interval.Equals( Interval );

                _defIntervals.Add( dbi );
            }

            UpdateDefaultIntervals( _lanHistory.Interval, true );
        }

        [ Range( typeof(TimeSpan), "0:02:00", "23:59:59", ErrorMessage =
            "The backup interval must be between 2 minutes and 23:59:59" ) ]
        public TimeSpan Interval
        {
            get => _lanHistory.Interval;

            set
            {
                if( Validate( value, "Interval" ) )
                {
                    bool changed = !_lanHistory.Interval.Equals( value );

                    if( changed )
                    {
                        _lanHistory.Interval = value;
                        RaisePropertyChanged( () => Interval );
                        RaisePropertyChanged( () => NextBackup );

                        UpdateDefaultIntervals( value );

                        Messenger.Default.Send<IntervalChangedMessage>( new IntervalChangedMessage()
                        {
                            Interval = value
                        } );
                    }
                }
            }
        }

        [ Range( 1, Int32.MaxValue, ErrorMessage = "The wake up period must be at least 1 minute" ) ]
        public int WakeUpTime
        {
            get => _lanHistory.WakeUpTime;

            set
            {
                value = value < Model.LanHistory.MinimumWakeUpMinutes ? Model.LanHistory.MinimumWakeUpMinutes : value;

                if( Validate( value, nameof(WakeUpTime) ) )
                {
                    bool changed = !_lanHistory.WakeUpTime.Equals( value );

                    if( changed )
                    {
                        _lanHistory.WakeUpTime = value;
                        RaisePropertyChanged( () => WakeUpTime );

                        Messenger.Default.Send<WakeUpChangedMessage>( new WakeUpChangedMessage()
                        {
                            WakeUpTime = value
                        } );
                    }
                }
            }
        }

        public List<DefaultBackupInterval> DefaultIntervals
        {
            get => _defIntervals;
            //set => Set<List<DefaultBackupInterval>>( ref _defIntervals, value );
        }

        public bool? ShareAccessible
        {
            get => _shareAccessible;

            set
            {
                bool changed = !_shareAccessible.Equals( value );

                _shareAccessible = value;

                if( changed )
                {
                    RaisePropertyChanged( () => ShareAccessible );
                    RaisePropertyChanged( () => ServerStatus );
                }
            }
        }

        public string StatusMesg
        {
            get
            {
                if( _lanHistory.IsValid )
                {
                    if( !_backupSucceeded ) return "Manual backup failed";

                    return $"Last: {_lanHistory.LastBackup: M/d/yyyy h:mm tt}";

                }

                return "Configuration required";
            }
        }

        public string NextBackup
        {
            get
            {
                if( !_lanHistory.IsValid )
                    return "Next: invalid configuration";

                if( !_backupTimer.Enabled )
                    return "Next: not running";

                return "Next: " + DateTime.Now.Add( _lanHistory.TimeRemaining ).ToString( "M/d/yyyy h:mm tt" );
            }
        }

        public string ServerStatus
        {
            get
            {
                if( ShareAccessible.HasValue )
                    return ShareAccessible.Value ? "Server online" : "Server offline";

                return "Checking server status...";
            }
        }

        public RelayCommand OpeningEventCommand { get; }
        public RelayCommand ExitApplicationCommand { get; }
        public RelayCommand BackupCommand { get; }
        public RelayCommand WakeServerCommand { get; }
        public RelayCommand<TimeSpan> SetBackupIntervalCommand { get; }

        private void OpeningEventHandler()
        {
            _lanHistory.LastBackup = _dataService.GetLastBackup();

            RaisePropertyChanged( () => StatusMesg );
        }

        private void Backup()
        {
            var logger = new ViewModelLocator().Logger;

            try
            {
                using( FileHistoryService fhs = new FileHistoryService() )
                {
                    fhs.Start();
                }

                logger.Information( "Started backup" );
                _backupSucceeded = true;
            }
            catch( Exception e )
            {
                _backupSucceeded = true;
                logger.Error( e, "Backup failed; message was {Message}" );

                RaisePropertyChanged( () => StatusMesg );
            }
        }

        private bool IsMainWindowOpen()
        {
            return Application.Current.MainWindow != null;
        }

        private void SendWakeOnLan()
        {
            var vml = new ViewModelLocator();

            (bool succeeded, string mesg) = vml.LanHistory.SendWakeOnLan();

            var logger = vml.Logger;

            if( succeeded ) logger.Information( mesg );
            else logger.Error( mesg );
        }

        private void SetBackupInterval( TimeSpan timeSpan )
        {
            Interval = timeSpan;
        }

        private void BackupResultMessageHandler( BackupResultMessage obj )
        {
            _backupSucceeded = obj?.Succeeded ?? false;

            RaisePropertyChanged( () => StatusMesg );
        }

        private void BackupTickMessageHandler( BackupTickMessage obj )
        {
            if( obj != null )
                RaisePropertyChanged( () => NextBackup );
        }

        private void EnableTimerMessageHandler( EnableTimerMessage obj )
        {
            RaisePropertyChanged( () => NextBackup );
        }

        private void ServerStatusMessageHandler( ServerStatusMessage obj )
        {
            if( obj != null )
                ShareAccessible = obj.ShareAccessible;
        }

        private void LanHistoryChangedMessageHandler( LanHistoryMessage obj )
        {
            RaisePropertyChanged( () => StatusMesg );
            RaisePropertyChanged( () => NextBackup );
        }

        private void PowerModeMessageHandler( PowerModeMessage obj )
        {
            if( obj != null && obj.Mode == PowerModes.Resume )
            {
                ShareAccessible = null;

                _lanHistory.LastBackup = _dataService.GetLastBackup();

                RaisePropertyChanged( () => StatusMesg );
                RaisePropertyChanged( () => NextBackup );
            }
        }

        private void UpdateDefaultIntervals( TimeSpan value, bool initial = false )
        {
            var newList = _defIntervals.Where( di => di.IsHitTestVisible ).ToList();

            bool matchFound = false;

            foreach( var defInterval in newList )
            {
                if( defInterval.Interval.Equals( value ) )
                {
                    defInterval.IsSelected = true;
                    matchFound = true;
                }
                else defInterval.IsSelected = false;
            }

            if( !matchFound )
                newList.Add(
                    new DefaultBackupInterval( SetBackupIntervalCommand )
                    {
                        Interval = value,
                        IsSelected = true,
                        IsHitTestVisible = false
                    } );

            _defIntervals = newList.OrderBy( di => di.Interval ).ToList();

            if( !initial ) RaisePropertyChanged( () => DefaultIntervals );
        }

    }
}
