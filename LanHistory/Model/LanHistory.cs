
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Timers;
using Olbert.JumpForJoy.Wpf;
using Olbert.LanHistory.Properties;

namespace Olbert.LanHistory.Model
{
    /// <summary>
    /// Defines Lan History Manager configuration, and provides wake-on-lan functionality
    /// </summary>
    public sealed class LanHistory : IDisposable
    {
        /// <summary>
        /// the minimum time, in minutes, needed by the backup server to come online and
        /// be ready to receive backups (2)
        /// </summary>
        public const int MinimumWakeUpMinutes = 2;

        /// <summary>
        /// the minimum interval between backups, in minutes (5)
        /// </summary>
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

        /// <summary>
        /// Creates an instance of LanHistory
        /// </summary>
        public LanHistory()
        {
            _timer = new Timer();
            _timer.Elapsed += TimerElapsed;
        }

        /// <summary>
        /// Flag indicating whether or not the backup target is a file share or a drive attached to the
        /// local machine (Lan History Manager only works with file shares)
        /// </summary>
        public bool IsRemote
        {
            get
            {
                if( !_isRemote.HasValue ) _isRemote = Settings.Default.IsRemote;

                return _isRemote.Value;
            }

            set => _isRemote = value;
        }

        /// <summary>
        /// The UNC path to the file share storing the backups
        /// </summary>
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

        /// <summary>
        /// The MAC address of the server hosting the file share used for backups; will be
        /// PhyiscalAddress.None if the MAC address is not known.
        /// 
        /// Setting this value updates MacAddressText property
        /// </summary>
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

        /// <summary>
        /// A text representation of the MAC address of the server hosting the file share for backups.
        /// 
        /// Setting this property updates the MacAddress property
        /// </summary>
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

        /// <summary>
        /// The date and time of the last backup
        /// </summary>
        public DateTime LastBackup
        {
            get
            {
                if( !_lastBackup.HasValue ) _lastBackup = Settings.Default.LastBackup;

                return _lastBackup.Value;
            }

            set => _lastBackup = value;
        }

        /// <summary>
        /// The interval between backups. This value cannot be set to less than MinimumBackupInterval.
        /// </summary>
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

                if( value < TimeRemaining ) TimeRemaining = value;

                _interval = value;
            }
        }

        /// <summary>
        /// The time remaining until the next backup is triggered
        /// </summary>
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

        /// <summary>
        /// The time needed by the backup server to wake up and be ready to receive backups.
        /// 
        /// This value cannot be set to less than MinimumWakeUpMinutes.
        /// </summary>
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

        /// <summary>
        /// Flag indicating whether or not the MacAddress property is defined and valid
        /// </summary>
        public bool MacAddressIsValid => MacAddress != null && !MacAddress.Equals( PhysicalAddress.None );

        /// <summary>
        /// Flag indicating whether or not the Lan History Manager configuration is valid
        /// </summary>
        public bool IsValid => MacAddressIsValid
                               && Settings.Default.BackupInterval > TimeSpan.Zero
                               && Settings.Default.WakeUp > 0;

        /// <summary>
        /// Saves the current Lan History Manager configuration to program settings
        /// </summary>
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

        /// <summary>
        /// Sends a wake-on-lan packet to the backup server, attempting to wake it
        /// </summary>
        /// <param name="repeats">the number of wake up packets to send; defaults to 3</param>
        /// <param name="msDelay">the delay, in milliseconds, between multiple wake up packets;
        /// defaults to 100 milliseconds</param>
        /// <returns>true and a null string if the first packet was sent successfully; false and
        /// an error message if it was not</returns>
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

        /// <summary>
        /// Disposes of the backup timer
        /// </summary>
        public void Dispose()
        {
            // optimizing these lines generates a compiler warning because the compiler isn't
            // smart enough to check null propagation operations
            if ( _timer != null ) _timer.Dispose();
        }
    }
}
