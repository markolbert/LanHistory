using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using GalaSoft.MvvmLight.Messaging;
using Olbert.LanHistory.Model;
using Microsoft.Win32;
using Olbert.LanHistory.Properties;
using Serilog;

namespace Olbert.LanHistory.ViewModel
{
    public class BackupTimer : IDisposable
    {
        public double BackupTimerTickMS = 30000;
        public double ShareTimerTickMS = 60000;
        public double KeepAwakeTimerTickMS = 5000;

        private readonly Model.LanHistory _lh;
        private readonly Timer _backupTimer;
        private readonly Timer _shareTimer;
        private readonly Timer _keepAwakeTimer;
        private bool? _shareAccessible;
        private readonly Model.LanHistory _lanHistory;

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

            _lanHistory = vml.LanHistory;

            _lh = vml.LanHistory ?? throw new NullReferenceException( "LanHistory" );
            Enabled = _lanHistory.IsValid;

            Messenger.Default.Register<EnableTimerMessage>( this, EnableTimerMessageHandler );
            //Messenger.Default.Register<IntervalChangedMessage>( this, IntervalChangedMessageHandler );
            //Messenger.Default.Register<WakeUpChangedMessage>( this, WakeUpChangedMessageHandler );

            SystemEvents.PowerModeChanged += PowerModeChangedHandler;
        }

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

                    Messenger.Default.Send<PowerModeMessage>( new PowerModeMessage() { Mode = e.Mode } );

                    break;

                case PowerModes.Suspend:
                    logger.LogDebugInformation( "Going to sleep" );

                    _lanHistory.Save();

                    Messenger.Default.Send<PowerModeMessage>(new PowerModeMessage() { Mode = e.Mode });

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
                Messenger.Default.Send<LanHistoryMessage>( lhcm );

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
                    Messenger.Default.Send<BackupResultMessage>( new BackupResultMessage() { Succeeded = succeeded } );
                }
                else
                {
                    if( !_keepAwakeTimer.Enabled )  _keepAwakeTimer.Start();
                }
            }

            if ( msRemaining <= BackupTimerTickMS ) _lanHistory.TimeRemaining = _lanHistory.Interval;
            else _lanHistory.TimeRemaining -= TimeSpan.FromMilliseconds( BackupTimerTickMS );

            Messenger.Default.Send<BackupTickMessage>(
                new BackupTickMessage() { TimeRemaining = _lanHistory.TimeRemaining } );
        }

        private void ShareTimerTickHandler( object sender, ElapsedEventArgs e )
        {
            PollLan();
        }

        private void KeepAwakeTimerTickHandler( object sender, ElapsedEventArgs e )
        {
            (bool succeeded, string mesg) = _lh.SendWakeOnLan( repeats : 1 );

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

                Messenger.Default.Send<ServerStatusMessage>( changed );
            }
        }

        private void EnableTimerMessageHandler( EnableTimerMessage obj )
        {
            if( obj != null ) Enabled = obj.Enabled;
        }

        //private void WakeUpChangedMessageHandler( WakeUpChangedMessage obj )
        //{
        //    if( obj != null ) _wakeUp = obj.WakeUpTime;
        //}

        //private void IntervalChangedMessageHandler( IntervalChangedMessage obj )
        //{
        //    if( obj != null ) _interval = obj.Interval;
        //}

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}

        protected virtual void Dispose( bool disposing )
        {
            if( disposing )
            {
                _backupTimer?.Dispose();
                _shareTimer?.Dispose();
                SystemEvents.PowerModeChanged -= PowerModeChangedHandler;
            }
        }

        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }
    }
}