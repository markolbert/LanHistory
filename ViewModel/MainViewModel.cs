using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Timers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LanHistory.Model;
using LanHistory.Properties;
using Microsoft.Win32;
using Serilog;

namespace LanHistory.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class MainViewModel : ValidatedViewModelBase, IDisposable
    {
        public const int DefaultWakeUpMinutes = 2;

        private readonly IDataService _dataService;
        private readonly ILogger _logger;

        private TimeSpan _interval = Settings.Default.BackupInterval;
        private int _wakeUp = Settings.Default.WakeUp;
        private PhysicalAddress _macAddress = PhysicalAddress.None;

        private readonly Timer _timer;

        public MainViewModel( IDataService dataService, ILogger logger )
        {
            _dataService = dataService;
            _logger = logger;

            GetConfig();

            _timer = new Timer();
            _timer.Elapsed += BackupTimerElapsedHandler;
            EnableTimer();

            SystemEvents.PowerModeChanged += PowerModeChangedHandler;

            WakeServerCommand = new RelayCommand( SendWakeOnLan, MacAddressIsValid );
            RefreshConfigCommand = new RelayCommand( GetConfig );
        }

        [Required(ErrorMessage = "The backup server's MAC address must be defined; is the server awake?")]
        public PhysicalAddress MacAddress
        {
            get => _macAddress;

            set
            {
                _macAddress = value;
                Set(ref _macAddress, value);

                if (Validate(value, nameof(MacAddress)))
                {
                    Settings.Default.MACAddressText = PhysicalAddressFormatter.Format(value);
                    Settings.Default.Save();

                    EnableTimer();
                }
            }
        }

        [Range( typeof(TimeSpan), "0:02:00", "23:59:59", ErrorMessage =
            "The backup interval must be between 2 minutes and 23:59:59" ) ]
        public TimeSpan Interval
        {
            get => _interval;

            set
            {
                Set( ref _interval, value );

                if( Validate( value, nameof(Interval) ) )
                    EnableTimer();
            }
        }

        [ Range( 1, Int32.MaxValue, ErrorMessage = "The wake up period must be at least 1 minute" ) ]
        public int WakeUpTime
        {
            get => _wakeUp;

            set
            {
                value = value < 0 ? DefaultWakeUpMinutes : value;

                Set( ref _wakeUp, value );

                if( Validate( value, nameof(WakeUpTime) ) )
                    EnableTimer();
            }
        }

        public DateTime LastBackup { get; private set; }
        public string ServerName { get; private set; }
        public IPAddress IPAddress { get; private set; }
        public bool IsRemote { get; private set; }

        public RelayCommand WakeServerCommand { get; }
        public RelayCommand RefreshConfigCommand { get; }

        protected virtual void Dispose( bool disposing )
        {
            if( disposing )
            {
                _timer?.Dispose();
                SystemEvents.PowerModeChanged -= PowerModeChangedHandler;
            }
        }

        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        private void BackupTimerElapsedHandler( object sender, ElapsedEventArgs e )
        {
            //SendWakeOnLan();
        }

        private void EnableTimer()
        {
            bool goodToGo = IsRemote && !MacAddress.Equals(PhysicalAddress.None) && Interval > TimeSpan.Zero &&
                            WakeUpTime > 0;

            if (goodToGo)
            {
                _timer.Interval = Interval.TotalMilliseconds;
                _timer.Start();
            }
        }

        private void PowerModeChangedHandler(object sender, PowerModeChangedEventArgs e)
        {
                switch ( e.Mode )
                {
                    case PowerModes.Resume:
                        _logger.Information("Resuming from sleep");
                        _timer.Start();

                        break;

                    case PowerModes.Suspend:
                        _logger.Information("Going to sleep");
                        _timer.Stop();

                        break;
                }
        }

        private bool MacAddressIsValid()
        {
            return MacAddress != null && !MacAddress.Equals( PhysicalAddress.None );
        }

        private void SendWakeOnLan()
        {
            try
            {
                List<byte> magicPacket = new List<byte>();

                for( int idx = 0; idx < 6; idx++ )
                {
                    magicPacket.Add( 0xFF );
                }

                byte[] macBytes = MacAddress.GetAddressBytes().Where( ( x, i ) => i < 6 ).ToArray();

                for( int idx = 0; idx < 16; idx++ )
                {
                    magicPacket.AddRange( macBytes );
                }

                var client = new UdpClient();
                int port = 7;

                client.Connect( System.Net.IPAddress.Broadcast, port );
                client.Send( magicPacket.ToArray(), magicPacket.Count );

                client.Close();

                _logger.Information( $"Sent wake-on-lan packet to {PhysicalAddressFormatter.Format( MacAddress )}" );
            }
            catch( Exception e )
            {
                _logger.Error(
                    $"Failed to send wake-on-lan packet to {PhysicalAddressFormatter.Format( MacAddress )}; message was {e.Message}" );
            }
        }

        private void GetConfig()
        {
            var fhi = _dataService.GetSystemConfig();

            if (fhi == null)
            {
                // see if we have a MAC address on file
                if (!String.IsNullOrEmpty(Settings.Default.MACAddressText))
                {
                    try
                    {
                        MacAddress = PhysicalAddress.Parse(Settings.Default.MACAddressText);
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                LastBackup = fhi.LastBackup;
                ServerName = fhi.ServerName;
                IPAddress = fhi.IPAddress;
                MacAddress = fhi.MacAddress;
                IsRemote = fhi.IsRemote;
            }

            Validate(ServerName, nameof(ServerName));
            Validate(MacAddress, nameof(MacAddress));
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}