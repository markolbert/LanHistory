﻿
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Olbert.JumpForJoy.WPF;
using Olbert.LanHistory.Model;

namespace Olbert.LanHistory.ViewModel
{
    /// <summary>
    /// The view model used by the task bar context menu UI for Lan History Manager
    /// </summary>
    public class ContextMenuViewModel : ValidatedViewModelBase
    {
        /// <summary>
        /// A view model for the backup interval menu items
        /// </summary>
        public class DefaultBackupInterval : ViewModelBase
        {
            private TimeSpan _interval;
            private bool _isSelected;

            /// <summary>
            /// Defines a backup interval menu element
            /// </summary>
            /// <param name="setBackupIntervalCommand">the MvvmLight RelayCommand that handles selection of 
            /// backup interval menu items</param>
            public DefaultBackupInterval( RelayCommand<TimeSpan> setBackupIntervalCommand )
            {
                SetBackupIntervalCommand = setBackupIntervalCommand;
            }

            /// <summary>
            /// the MvvmLight RelayCommand that handles selection of 
            /// backup interval menu items
            /// </summary>
            public RelayCommand<TimeSpan> SetBackupIntervalCommand { get; }

            /// <summary>
            /// The interval between backups. This is an MvvmLight dependency property.
            /// </summary>
            public TimeSpan Interval
            {
                get => _interval;
                set => Set<TimeSpan>( ref _interval, value );
            }

            /// <summary>
            /// Flag indicating whether this menu item is selected. This is an MvvmLight dependency property.
            /// </summary>
            public bool IsSelected
            {
                get => _isSelected;
                set => Set<bool>( ref _isSelected, value );
            }

            /// <summary>
            /// Flag indicating whether or not this menu item is selectable.
            /// </summary>
            public bool IsHitTestVisible { get; set; } = true;
        }

        private readonly IDataService _dataService;
        private bool _backupSucceeded = true;
        private bool? _shareAccessible;
        private readonly BackupTimer _backupTimer;
        private readonly Model.LanHistory _lanHistory;
        private List<DefaultBackupInterval> _defIntervals = new List<DefaultBackupInterval>();

        /// <summary>
        /// Creates an instance of the view model
        /// </summary>
        public ContextMenuViewModel()
        {
            var vml = new ViewModelLocator();
            _dataService = vml.DataService ?? throw new NullReferenceException( "IDataService" );
            _lanHistory = vml.LanHistory ?? throw new NullReferenceException( "LanHistory" );
            _backupTimer = vml.BackupTimer ?? throw new NullReferenceException( "BackupTimer" );

            OpeningEventCommand = new RelayCommand( OpeningEventHandler );
            ExitApplicationCommand = new RelayCommand( () => Application.Current.Shutdown() );
            AboutCommand = new RelayCommand( ShowAbout );
            HelpCommand = new RelayCommand(ShowHelp);

            BackupCommand = new RelayCommand( Backup, () => _lanHistory.IsValid && ( ShareAccessible ?? false ) );
            WakeServerCommand = new RelayCommand( SendWakeOnLan, () => _lanHistory.MacAddressIsValid );
            SetBackupIntervalCommand = new RelayCommand<TimeSpan>( SetBackupInterval );

            Messenger.Default.Register<BackupResultMessage>( this, BackupResultMessageHandler );
            Messenger.Default.Register<BackupTickMessage>( this, BackupTickMessageHandler );
            //Messenger.Default.Register<EnableTimerMessage>( this, EnableTimerMessageHandler );
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

            UpdateDefaultIntervals( _lanHistory.Interval );
        }

        /// <summary>
        /// The interval between backups, which must be between 2 minutes and 23 hours, 59 minutes
        /// and 59 seconds.
        /// 
        /// This is an MvvmLight dependency property.
        /// </summary>
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
                        RaisePropertyChanged(() => DefaultIntervals);

                        //Messenger.Default.Send<IntervalChangedMessage>( new IntervalChangedMessage()
                        //{
                        //    Interval = value
                        //} );
                    }
                }
            }
        }

        /// <summary>
        /// The time needed, in minutes, for the backup server to wake up and become able to receive a backup.
        /// 
        /// This value must be at least 2 minutes.
        /// 
        /// This is an MvvmLight dependency property.
        /// </summary>
        [Range( Model.LanHistory.MinimumWakeUpMinutes, Int32.MaxValue, ErrorMessage = "The wake up period must be at least 2 minutes" ) ]
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

                        //Messenger.Default.Send<WakeUpChangedMessage>( new WakeUpChangedMessage()
                        //{
                        //    WakeUpTime = value
                        //} );
                    }
                }
            }
        }

        /// <summary>
        /// The default backup intervals
        /// </summary>
        public List<DefaultBackupInterval> DefaultIntervals
        {
            get => _defIntervals;
            //set => Set<List<DefaultBackupInterval>>( ref _defIntervals, value );
        }

        /// <summary>
        /// Flag indicating whether or not the backup server is accessible:
        /// 
        /// - true means it is;
        /// - false means it isn't;
        /// - null means its state is unknown
        /// 
        /// This is an MvvmLight dependency property.
        /// </summary>
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
                    BackupCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Status message about the last backup
        /// </summary>
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

        /// <summary>
        /// Status message about the next scheduled backup
        /// </summary>
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

        /// <summary>
        /// Message about the backup server's status
        /// </summary>
        public string ServerStatus
        {
            get
            {
                if( ShareAccessible.HasValue )
                    return ShareAccessible.Value ? "Server online" : "Server offline";

                return "Checking server status...";
            }
        }

        /// <summary>
        /// MvvmLight RelayCommand triggered when the context menu is opened
        /// </summary>
        public RelayCommand OpeningEventCommand { get; }

        /// <summary>
        /// MvvmLight RelayCommand triggered when the user requests the about menu item
        /// </summary>
        public RelayCommand AboutCommand { get; }

        /// <summary>
        /// MvvmLight RelayCommand triggered when the user requests the help menu item
        /// </summary>
        public RelayCommand HelpCommand { get; }

        /// <summary>
        /// MvvmLight RelayCommand triggered when the user requests the application to exit
        /// </summary>
        public RelayCommand ExitApplicationCommand { get; }

        /// <summary>
        /// MvvmLight RelayCommand triggered when the user requests a manual backup
        /// </summary>
        public RelayCommand BackupCommand { get; }

        /// <summary>
        /// MvvmLight RelayCommand triggered when the user makes a "wake server" request
        /// </summary>
        public RelayCommand WakeServerCommand { get; }

        /// <summary>
        /// MvvmLight RelayCommand triggered when a backup interval is selected through the UI
        /// </summary>
        public RelayCommand<TimeSpan> SetBackupIntervalCommand { get; }

        private void OpeningEventHandler()
        {
            _lanHistory.LastBackup = _dataService.GetLastBackup();

            RaisePropertyChanged( () => StatusMesg );
        }

        private void ShowAbout()
        {
            var exeAss = Assembly.GetExecutingAssembly().GetName();
            var exePath = Assembly.GetEntryAssembly().Location;
            var verInfo = String.IsNullOrEmpty( exePath ) ? null : FileVersionInfo.GetVersionInfo( exePath );

            StringBuilder sb = new StringBuilder();

            sb.Append( "Lan History Manager" );
            sb.Append( $"\nv{exeAss.Version.ToString()}" );

            if( verInfo != null )
            {
                sb.Append( $"\n\n{verInfo.CompanyName}" );
                sb.Append( $"\n\n{verInfo.LegalCopyright}" );
            }

            new J4JMessageBox().Title( "About Lan History Manager" )
                .Message( sb.ToString() )
                .ButtonText( "Okay" )
                .ShowMessageBox();
        }

        private void ShowHelp()
        {
            Process.Start( "http://www.JumpForJoySoftware.com/Lan-History-Manager" );
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

        //private void EnableTimerMessageHandler( EnableTimerMessage obj )
        //{
        //    RaisePropertyChanged( () => NextBackup );
        //}

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

        private void UpdateDefaultIntervals( TimeSpan value )
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
        }

    }
}
