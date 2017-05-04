using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Timers;
using Olbert.LanHistory.Properties;

namespace Olbert.LanHistory.Model
{
    public class LanHistory : IDisposable
    {
        public const int MinimumWakeUpMinutes = 2;
        public static TimeSpan MinimumBackupInterval = TimeSpan.FromMinutes( 5 );

        private PhysicalAddress _macAddress;
        private string _macAddrText;
        private readonly Timer _timer;
        private int _numToSend = 0;
        private string _uncPath;
        private TimeSpan _interval = TimeSpan.Zero;
        private int _wakeUp = 0;
        private bool? _isRemote;
        private TimeSpan _remaining = TimeSpan.Zero;
        private DateTime? _lastBackup;

        public LanHistory()
        {
            _timer = new Timer();
            _timer.Elapsed += TimerElapsed;
        }

        public bool IsRemote
        {
            get
            {
                if( !_isRemote.HasValue ) _isRemote = Settings.Default.IsRemote;

                return _isRemote.Value;
            }

            set => _isRemote = value;
        }

        public string UNCPath
        {
            get
            {
                if( String.IsNullOrEmpty( _uncPath ) )
                    _uncPath = Settings.Default.UNCPath;

                return _uncPath;
            }

            set => _uncPath = value;
        }

        public PhysicalAddress MacAddress
        {
            get
            {
                if( _macAddress == null || _macAddress.Equals(PhysicalAddress.None) )
                {
                    _macAddress = PhysicalAddress.None;

                    try
                    {
                        _macAddress = PhysicalAddress.Parse( MACAddressText );
                    }
                    catch
                    {
                    }
                }

                return _macAddress;
            }

            set => MACAddressText = value?.ToString();
        }

        public string MACAddressText
        {
            get
            {
                if( String.IsNullOrEmpty( _macAddrText ) )
                    _macAddrText = Settings.Default.MACAddressText;

                return _macAddrText;

            }

            set
            {
                _macAddrText = value;

                try
                {
                    _macAddress = PhysicalAddress.Parse( value );
                }
                catch
                {
                }
            }
        }

        public DateTime LastBackup
        {
            get
            {
                if( !_lastBackup.HasValue ) _lastBackup = Settings.Default.LastBackup;

                return _lastBackup.Value;
            }

            set => _lastBackup = value;
        }

        public TimeSpan Interval
        {
            get
            {
                if( _interval.Equals( TimeSpan.Zero ) )
                    _interval = Settings.Default.BackupInterval;

                if( _interval < MinimumBackupInterval ) _interval = MinimumBackupInterval;

                return _interval;
            }

            set
            {
                if( value < MinimumBackupInterval ) value = MinimumBackupInterval;

                _interval = value;
            }
        }

        public TimeSpan TimeRemaining
        {
            get
            {
                if (_remaining.Equals(TimeSpan.Zero))
                    _remaining = Settings.Default.TimeRemaining;

                return _remaining;
            }

            set => _remaining = value;
        }

        public int WakeUpTime
        {
            get
            {
                if( _wakeUp < MinimumWakeUpMinutes ) _wakeUp = MinimumWakeUpMinutes;

                return _wakeUp;
            }

            set
            {
                if( value < MinimumWakeUpMinutes ) value = MinimumWakeUpMinutes;

                _wakeUp = value;
            }
        }

        public bool MacAddressIsValid => MacAddress != null && !MacAddress.Equals( PhysicalAddress.None );

        public bool IsValid => MacAddressIsValid
                               && Settings.Default.BackupInterval > TimeSpan.Zero
                               && Settings.Default.WakeUp > 0;

        public void Save()
        {
            Settings.Default.BackupInterval = Interval;
            Settings.Default.IsRemote = IsRemote;
            Settings.Default.LastBackup = LastBackup;
            Settings.Default.MACAddressText = MACAddressText;
            Settings.Default.TimeRemaining = TimeRemaining;
            Settings.Default.UNCPath = UNCPath;
            Settings.Default.WakeUp = WakeUpTime;

            Settings.Default.Save();
        }

        public (bool succeeded, string mesg) SendWakeOnLan( int repeats = 3, int msDelay = 100 )
        {
            repeats = repeats <= 0 ? 1 : repeats;
            msDelay = msDelay <= 0 ? 100 : msDelay;

            (bool succeeded, string mesg) = SendWakeOnLanInternal();

            if( repeats > 1 )
            {
                _numToSend = repeats - 1;
                _timer.Interval = msDelay;

                _timer.Start();
            }

            return (succeeded, mesg);
        }

        private (bool succeeded, string mesg) SendWakeOnLanInternal()
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

                return (true, $"Sent wake-on-lan packet to {PhysicalAddressFormatter.Format( MacAddress )}");
            }
            catch( Exception e )
            {
                return (false,
                    $"Failed to send wake-on-lan packet to {PhysicalAddressFormatter.Format( MacAddress )}; message was {e.Message}"
                    );
            }
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            SendWakeOnLanInternal();

            _numToSend--;
            _timer.Enabled = _numToSend > 0;
        }

        protected virtual void Dispose( bool disposing )
        {
            if( disposing )
            {
                _timer?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }
    }
}
