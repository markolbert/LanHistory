using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using GalaSoft.MvvmLight;
using LanHistory.Model;
using LanHistory.Properties;
using Serilog;

namespace LanHistory.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class MainViewModel : ValidatedViewModelBase
    {
        private DateTime _lastBackup = Settings.Default.Configuration.LastBackup;
        private TimeSpan _interval = Settings.Default.Configuration.Interval;
        private string _serverName = Settings.Default.Configuration.ServerName;
        private string _ipAddress = Settings.Default.Configuration.IPAddress;
        private string _macAddress = Settings.Default.Configuration.MacAddress;
        private bool _isRemote = Settings.Default.Configuration.IsRemote;
        private int _wakeUp = Settings.Default.Configuration.WakeUpTime;

        private readonly Dictionary<string, ICollection<string>> _validationErrors =
            new Dictionary<string, ICollection<string>>();

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel( IDataService dataService )
        {
            var (fhi, log) = dataService.GetSystemConfig();

            if( fhi != null )
            {
                LastBackup = fhi.LastBackup;
                Interval = fhi.Interval;
                ServerName = fhi.ServerName;
                IPAddress = fhi.IPAddress;
                MacAddress = fhi.MacAddress;
                IsRemote = fhi.IsRemote;
                WakeUpTime = fhi.WakeUpTime;
            }
        }

        public DateTime LastBackup
        {
            get => _lastBackup;
            set => Set( ref _lastBackup, value );
        }

        [ Range( typeof(TimeSpan), "0:02:00", "23:59:59", ErrorMessage =
            "The backup interval must be between 2 minutes and 23:59:59" ) ]
        public TimeSpan Interval
        {
            get => _interval;

            set
            {
                Set( ref _interval, value );
                Validate( value, nameof(Interval) );
            }
        }

        [ Required( ErrorMessage = "The backup server must be defined" ) ]
        public string ServerName
        {
            get => _serverName;

            set
            {
                Set( ref _serverName, value );
                Validate( value, nameof(ServerName) );
            }
        }

        public string IPAddress
        {
            get => _ipAddress;
            set => Set( ref _ipAddress, value );
        }

        [Required(ErrorMessage = "The backup server's MAC address must be defined")]
        public string MacAddress
        {
            get => _macAddress;

            set
            {
                Set( ref _macAddress, value );
                Validate( value, nameof(MacAddress) );
            }
        }

        public bool IsRemote
        {
            get => _isRemote;
            set => Set( ref _isRemote, value );
        }

        public int WakeUpTime
        {
            get => _wakeUp;

            set
            {
                value = value < 0 ? UpTimeMonitor.DefaultWakeUpMinutes : value;
                Set( ref _wakeUp, value );
            }
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}