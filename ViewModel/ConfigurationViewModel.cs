using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.NetworkInformation;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Olbert.LanHistory.Model;
using Serilog;

namespace Olbert.LanHistory.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class ConfigurationViewModel : ValidatedViewModelBase
    {
        public const int DefaultWakeUpMinutes = 2;

        private readonly IDataService _dataService;
        private readonly ILogger _logger;

        private readonly LanHistoryModel _lhModel;
        private FileHistoryModel _fhModel;
        private string _error;

        private PhysicalAddress _macAddress;
        private TimeSpan _interval;
        private int _wakeUp;

        public ConfigurationViewModel( IDataService dataService, ILogger logger, LanHistoryModel lhModel )
        {
            _dataService = dataService ?? throw new NullReferenceException( nameof(dataService) );
            _logger = logger ?? throw new NullReferenceException( nameof(logger) );
            _lhModel = lhModel ?? throw new NullReferenceException( nameof(lhModel) );

            _macAddress = _lhModel.MacAddress;
            _interval = _lhModel.Interval;
            _wakeUp = _lhModel.WakeUpTime;

            GetConfigCommand = new RelayCommand( GetConfig );
            SaveCommand = new RelayCommand<IClosable>( Save, ( x ) => IsValid );

            GetConfig();
            Messenger.Default.Send( new EnableTimerMessage() { Enabled = IsValid } );
        }

        [ Required( ErrorMessage = "The backup server's MAC address must be defined; is the server awake?" ) ]
        public PhysicalAddress MacAddress
        {
            get => _macAddress;

            set
            {
                Set<PhysicalAddress>( ref _macAddress, value );

                if( Validate( value, nameof(MacAddress) ) )
                    Messenger.Default.Send( new EnableTimerMessage() { Enabled = IsValid } );
            }
        }

        [ Range( typeof(TimeSpan), "0:02:00", "23:59:59", ErrorMessage =
            "The backup interval must be between 2 minutes and 23:59:59" ) ]
        public TimeSpan Interval
        {
            get => _interval;

            set
            {
                Set<TimeSpan>( ref _interval, value );

                if( Validate( value, nameof(Interval) ) )
                    Messenger.Default.Send( new EnableTimerMessage() { Enabled = IsValid } );
            }
        }

        [ Range( 1, Int32.MaxValue, ErrorMessage = "The wake up period must be at least 1 minute" ) ]
        public int WakeUpTime
        {
            get => _wakeUp;

            set
            {
                value = value < 0 ? DefaultWakeUpMinutes : value;
                Set<int>( ref _wakeUp, value );

                if( Validate( value, nameof(WakeUpTime) ) )
                    Messenger.Default.Send( new EnableTimerMessage() { Enabled = IsValid } );
            }
        }

        public string Error
        {
            get => _error;

            set
            {
                bool changed = !_error.Equals( value );
                _error = value;
                if( changed ) RaisePropertyChanged( () => Error );
            }
        }

        public RelayCommand GetConfigCommand { get; }
        public RelayCommand<IClosable> SaveCommand { get; }

        public DateTime LastBackup => _fhModel.LastBackup;
        public string ServerName => _fhModel.ServerName;
        public IPAddress IPAddress => _fhModel.IPAddress;
        public bool IsRemote => _fhModel.IsRemote;

        public bool IsValid => IsRemote && !MacAddress.Equals( PhysicalAddress.None ) && Interval > TimeSpan.Zero &&
                               WakeUpTime > 0;

        private void GetConfig()
        {
            var fhm = _dataService.GetSystemConfig();

            if( fhm == null )
            {
                _fhModel = new FileHistoryModel();

                // see if we have a MAC address stored in settings
                if( !_lhModel.MacAddress.Equals( PhysicalAddress.None ) )
                    MacAddress = _lhModel.MacAddress;
            }
            else
            {
                _fhModel = fhm;
                MacAddress = _fhModel.MacAddress;
            }

            Validate( ServerName, nameof(ServerName) );
            Validate( MacAddress, nameof(MacAddress) );
        }

        private void Save( IClosable window )
        {
            _lhModel.WakeUpTime = WakeUpTime;
            _lhModel.MacAddress = MacAddress;
            _lhModel.Interval = Interval;

            Messenger.Default.Send<ConfigurationChangedMessage>( new ConfigurationChangedMessage()
            {
                Interval = Interval,
                MacAddress = MacAddress,
                WakeUpTime = WakeUpTime
            } );

            window.Close();
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}