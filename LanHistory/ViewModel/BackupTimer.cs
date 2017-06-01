
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;
using System.Timers;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Serilog;

namespace Olbert.LanHistory.ViewModel
{
    /// <summary>
    /// The central timer for Lan History Manager; checks server status, triggers backups, and keeps the
    /// backup server awake while a backup is pending (because the backup server might try to sleep
    /// between the time it is woken up to receive a backup and when the backup actually starts).
    /// </summary>
    public sealed class BackupTimer : IDisposable
    {
        /// <summary>
        /// The interval, in milliseconds, between checks to determine whether or not the backup
        /// state should change, progressing from "awaiting next backup", to "wake server", to "trigger backup",
        /// and back to "awaiting next backup" (30,000 milliseconds)
        /// </summary>
        public const double BackupTimerTickMS = 30000;

        /// <summary>
        /// The interval, in milliseconds, between checks to see if the backup server is accessible
        /// (60,000 milliseconds)
        /// </summary>
        public const double ShareTimerTickMS = 60000;

        /// <summary>
        /// The interval, in milliseconds, between sending "wake on lan" packets to the backup server
        /// to keep it awake (5,000 milliseconds)
        /// </summary>
        public const double KeepAwakeTimerTickMS = 5000;

        private readonly Timer _backupTimer;
        private readonly Timer _shareTimer;
        private readonly Timer _keepAwakeTimer;
        private bool? _shareAccessible;
        private readonly Model.LanHistory _lanHistory;

        /// <summary>
        /// Creates an instance of BackupTimer and starts the actual backup timer and the check server 
        /// status timer
        /// </summary>
        public BackupTimer()
        {
            _backupTimer = new Timer( BackupTimerTickMS );
            _backupTimer.Elapsed += BackupTimerTickHandler;
            _backupTimer.Start();

            PollLan();
            _shareTimer = new Timer( ShareTimerTickMS );
            _shareTimer.Elapsed += ShareTimerTickHandler;
            _shareTimer.Start();

            _keepAwakeTimer = new Timer(KeepAwakeTimerTickMS);
            _keepAwakeTimer.Elapsed += KeepAwakeTimerTickHandler;

            ViewModelLocator vml = new ViewModelLocator();

            _lanHistory = vml.LanHistory ?? throw new NullReferenceException("LanHistory"); 
            Enabled = _lanHistory.IsValid;

            //Messenger.Default.Register<EnableTimerMessage>( this, EnableTimerMessageHandler );

            SystemEvents.PowerModeChanged += PowerModeChangedHandler;
        }

        /// <summary>
        /// Flag indicating whether or not the BackupTimer is enabled. If false, that generally
        /// means somethings is misconfigured
        /// </summary>
        public bool Enabled { get; private set; }

        private void PowerModeChangedHandler( object sender, PowerModeChangedEventArgs e )
        {
            ILogger logger = new ViewModelLocator().Logger;

            switch( e.Mode )
            {
                case PowerModes.Resume:
                    logger.LogDebugInformation( "Resuming from sleep" );

                    PollLan();
                    _backupTimer.Start();

                    Application.Current.Dispatcher.Invoke(
                        () => Messenger.Default.Send<PowerModeMessage>( new PowerModeMessage() { Mode = e.Mode } ) );

                    break;

                case PowerModes.Suspend:
                    logger.LogDebugInformation( "Going to sleep" );

                    _lanHistory.Save();

                    Application.Current.Dispatcher.Invoke( 
                        () => Messenger.Default.Send<PowerModeMessage>( new PowerModeMessage() { Mode = e.Mode } ) );

                    _backupTimer.Stop();

                    break;
            }
        }

        private void BackupTimerTickHandler( object sender, ElapsedEventArgs e )
        {
            if( !Enabled ) return;

            var vml = new ViewModelLocator();
            ILogger logger = vml.Logger;

            // always update backup info
            LanHistoryMessage lhcm = LanHistoryMessage.GetChanged();

            if( lhcm != null )
                Application.Current.Dispatcher.Invoke( 
                    () => Messenger.Default.Send<LanHistoryMessage>( lhcm ) );

            double msRemaining = _lanHistory.TimeRemaining.TotalMilliseconds;

            if( msRemaining <= BackupTimerTickMS + _lanHistory.WakeUpTime * 60000 )
            {
                if( msRemaining <= BackupTimerTickMS )
                {
                    bool succeeded = false;

                    try
                    {
                        using( FileHistoryService fhs = new FileHistoryService() )
                        {
                            fhs.Start();
                        }

                        logger.LogDebugInformation( "Sent start backup signal" );
                        succeeded = true;
                    }
                    catch( Exception exception )
                    {
                        logger.Error( exception, "Backup failed; message was {Message}" );
                    }

                    _keepAwakeTimer.Stop();

                    Application.Current.Dispatcher.Invoke(
                        () => Messenger.Default.Send<BackupResultMessage>(
                            new BackupResultMessage() { Succeeded = succeeded } ) );
                }
                else
                {
                    if( !_keepAwakeTimer.Enabled )  _keepAwakeTimer.Start();
                }
            }

            if ( msRemaining <= BackupTimerTickMS ) _lanHistory.TimeRemaining = _lanHistory.Interval;
            else _lanHistory.TimeRemaining -= TimeSpan.FromMilliseconds( BackupTimerTickMS );

            Application.Current.Dispatcher.Invoke(
                () =>
                    Messenger.Default.Send<BackupTickMessage>(
                        new BackupTickMessage() { TimeRemaining = _lanHistory.TimeRemaining } ) );
        }

        private void ShareTimerTickHandler( object sender, ElapsedEventArgs e )
        {
            PollLan();
        }

        private void KeepAwakeTimerTickHandler( object sender, ElapsedEventArgs e )
        {
            (bool succeeded, string mesg) = _lanHistory.SendWakeOnLan( repeats : 1 );

            var logger = new ViewModelLocator().Logger;

            if( succeeded ) logger.LogDebugInformation( mesg );
            else logger.Error( mesg );
        }

        private async void PollLan()
        {
            var changed =
                await ServerStatusMessage.GetChanged( _shareAccessible );

            if( changed != null )
            {
                _shareAccessible = changed.ShareAccessible;

                Application.Current.Dispatcher.Invoke( () => Messenger.Default.Send<ServerStatusMessage>( changed ) );
            }
        }

        //private void EnableTimerMessageHandler( EnableTimerMessage obj )
        //{
        //    if( obj != null ) Enabled = obj.Enabled;
        //}

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}

        /// <summary>
        /// Disposes of the timers
        /// </summary>
        public void Dispose()
        {
            // optimizing these lines generates a compiler warning because the compiler isn't
            // smart enough to check null propagation operations
            if( _backupTimer != null ) _backupTimer.Dispose();
            if( _shareTimer != null ) _shareTimer.Dispose();
            if( _keepAwakeTimer != null ) _keepAwakeTimer.Dispose();
        }
    }
}