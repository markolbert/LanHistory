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
    public class LanHistoryModel : IDisposable
    {
        private PhysicalAddress _macAddress;
        private string _macAddrText;
        private Timer _timer;
        private int _numToSend = 0;

        public LanHistoryModel()
        {
            _timer = new Timer();
            _timer.Elapsed += TimerElapsed;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            SendWakeOnLanInternal();

            _numToSend--;
            _timer.Enabled = _numToSend > 0;
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

                Settings.Default.MACAddressText = value;
                Settings.Default.Save();

                try
                {
                    _macAddress = PhysicalAddress.Parse( value );
                }
                catch
                {
                }
            }
        }

        public TimeSpan Interval
        {
            get => Settings.Default.BackupInterval;

            set
            {
                Settings.Default.BackupInterval = value;
                Settings.Default.Save();
            }
        }

        public int WakeUpTime
        {
            get => Settings.Default.WakeUp;

            set
            {
                Settings.Default.WakeUp = value;
                Settings.Default.Save();
            }
        }

        public bool MacAddressIsValid => MacAddress != null && !MacAddress.Equals( PhysicalAddress.None );
        public bool IsValid => MacAddressIsValid && Interval > TimeSpan.Zero && WakeUpTime > 0;

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
