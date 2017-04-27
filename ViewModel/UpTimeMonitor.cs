using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using LanHistory.Model;
using Microsoft.Win32;
using Serilog;

namespace LanHistory.ViewModel
{
    public class UpTimeMonitor : IDisposable
    {
        public const int DefaultWakeUpMinutes = 2;
        public static TimeSpan DefaultInterval = TimeSpan.FromMinutes( 60 );

        private readonly Timer _timer;
        private TimeSpan _interval = TimeSpan.FromMinutes(60);
        private int _wakeUp = 2;
        private bool _isRemote = false;
        private string _macAddress = null;
        private readonly EventLog _eventLog;

        public UpTimeMonitor( EventLog eventLog )
        {
            _eventLog = eventLog;

            _timer = new Timer();
            _timer.Elapsed += BackupTimerElapsedHandler;

            SystemEvents.PowerModeChanged += PowerModeChangedHandler;
        }

        public string MacAddress
        {
            get => _macAddress;

            set
            {
                _macAddress = value;
                EnableTimer();
            }
        }

        public TimeSpan Interval
        {
            get => _interval;
            set
            {
                if( value != _interval )
                    _timer.Interval = value.TotalSeconds;

                _interval = value;

                EnableTimer();
            }
        }

        public int WakeUpTime
        {
            get => _wakeUp;

            set
            {
                _wakeUp = value <= 0 ? DefaultWakeUpMinutes : value; 
                EnableTimer();
            }
        }

        public bool IsRemote
        {
            get => _isRemote;

            set
            {
                _isRemote = value;
                EnableTimer();
            }
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }

        private void BackupTimerElapsedHandler(object sender, ElapsedEventArgs e)
        {
            StringWriter sw = new StringWriter();
            PhysicalAddress macAddress = PhysicalAddress.None;
            EventLogEntryType eventType = EventLogEntryType.Information;

            if (!IsRemote)
            {
                sw.WriteLine("Backup system is not a network share, can't be woken up");
                eventType = EventLogEntryType.Error;
            }
            else
            {
                try
                {
                    macAddress = PhysicalAddress.Parse(MacAddress.Replace(':', '-').Replace('.', '-'));
                }
                catch
                {
                    sw.WriteLine("Unknown MAC address, can't send wake-on-lan packet");
                    eventType = EventLogEntryType.Error;
                }
            }

            //eventType = SendWakeOnLan( macAddress, sw );
            sw.WriteLine("Would have sent wake-on-LAN packet");

            _eventLog.WriteEntry(sw.ToString(), eventType);
        }

        private void EnableTimer()
        {
            _timer.Enabled = _isRemote && !String.IsNullOrEmpty( MacAddress ) && _interval > TimeSpan.Zero &&
                             _wakeUp > 0;
        }

        private void PowerModeChangedHandler(object sender, PowerModeChangedEventArgs e)
        {
            switch( e.Mode )
            {
                case PowerModes.Resume:
                    if( _interval != TimeSpan.Zero ) _timer.Start();
                    break;

                case PowerModes.Suspend:
                    _timer.Stop();
                    break;
            }
        }

        private EventLogEntryType SendWakeOnLan( PhysicalAddress macAddress, StringWriter sw )
        {
            if( macAddress.Equals( PhysicalAddress.None ) ) return EventLogEntryType.Error;

            List<byte> magicPacket = new List<byte>();

            for (int idx = 0; idx < 6; idx++)
            {
                magicPacket.Add(0xFF);
            }

            byte[] macBytes = macAddress.GetAddressBytes();

            for (int idx = 0; idx < 16; idx++)
            {
                magicPacket.AddRange(macBytes);
            }

            try
            {
                var client = new UdpClient();
                int port = 7;

                client.Connect(System.Net.IPAddress.Broadcast, port);
                client.Send(magicPacket.ToArray(), magicPacket.Count);

                client.Close();

                sw.WriteLine(
                    $"sent magic packet for MAC address {MacAddress} to port {port} on {System.Net.IPAddress.Broadcast.ToString()}");
            }
            catch (Exception exception)
            {
                sw.WriteLine($"Wake-on-LAN packet not sent; message was {exception.Message}");
                return EventLogEntryType.Error;
            }

            return EventLogEntryType.Information;
        }
    }
}
