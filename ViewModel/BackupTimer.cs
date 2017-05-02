using System;
using System.Timers;
using FileHistory;
using GalaSoft.MvvmLight.Messaging;
using Olbert.LanHistory.Model;
using Microsoft.Win32;
using Serilog;

namespace Olbert.LanHistory.ViewModel
{
    public class BackupTimer : IDisposable
    {
        public double TimerTickMS = 30000;

        private readonly LanHistoryModel _lhModel;
        private readonly ILogger _logger;
        private readonly Timer _timer;

        public BackupTimer()
        {
            ViewModelLocator vml = new ViewModelLocator();

            _logger = vml.Logger ?? throw new NullReferenceException("Logger");
            _lhModel = vml.LanHistoryModel ?? throw new NullReferenceException( "LanHistoryModel" );

            TimeRemaining = _lhModel.Interval;

            _timer = new Timer( TimerTickMS );
            _timer.Elapsed += TimerTickHandler;
            _timer.Start();

            Enabled = _lhModel.IsValid;

            Messenger.Default.Register<EnableTimerMessage>( this, EnableTimerMessageHandler );
            Messenger.Default.Register<ConfigurationChangedMessage>( this, ConfigurationChangedMessageHandler );

            SystemEvents.PowerModeChanged += PowerModeChangedHandler;
        }

        public TimeSpan TimeRemaining { get; private set; }
        public bool Enabled { get; private set; }
        public bool WakeUpSent { get; private set; }

        private void PowerModeChangedHandler( object sender, PowerModeChangedEventArgs e )
        {
            switch( e.Mode )
            {
                case PowerModes.Resume:
                    _logger.Information( "Resuming from sleep" );
                    _timer.Start();

                    break;

                case PowerModes.Suspend:
                    _logger.Information( "Going to sleep" );
                    _timer.Stop();

                    break;
            }
        }

        private void TimerTickHandler( object sender, ElapsedEventArgs e )
        {
            if (!Enabled) return;

            double msRemaining = TimeRemaining.TotalMilliseconds;

            if( msRemaining <= TimerTickMS + _lhModel.WakeUpTime * 60000 )
            {
                if( msRemaining <= TimerTickMS )
                {
                    bool succeeded = false;

                    try
                    {
                        using (FileHistoryService fhs = new FileHistoryService())
                        {
                            fhs.Start();
                        }

                        _logger.Information("Started backup");
                        succeeded = true;
                    }
                    catch (Exception exception)
                    {
                        _logger.Error(exception, "Backup failed; message was {Message}");
                    }

                    Messenger.Default.Send<BackupResultMessage>( new BackupResultMessage() { Succeeded = succeeded } );

                    WakeUpSent = false;
                }
                else
                {
                    if( !WakeUpSent )
                    {
                        (bool succeeded, string mesg) = _lhModel.SendWakeOnLan();

                        if( succeeded )
                        {
                            WakeUpSent = true;

                            _logger.Information( mesg );
                        }
                        else _logger.Error( mesg );
                    }
                }
            }

            if( msRemaining <= TimerTickMS ) TimeRemaining = _lhModel.Interval;
            else TimeRemaining -= TimeSpan.FromMilliseconds( TimerTickMS );

            Messenger.Default.Send<BackupTimerTickMessage>(
                new BackupTimerTickMessage() { TimeRemaining = TimeRemaining } );
        }

        private void EnableTimerMessageHandler( EnableTimerMessage obj )
        {
            if( obj != null )
                Enabled = obj.Enabled;
        }

        private void ConfigurationChangedMessageHandler(ConfigurationChangedMessage obj)
        {
            if( obj != null && obj.Interval < TimeRemaining ) TimeRemaining = obj.Interval;
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();
                SystemEvents.PowerModeChanged -= PowerModeChangedHandler;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}