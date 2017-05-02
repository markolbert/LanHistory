using System;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;
using FileHistory;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Olbert.LanHistory.Model;
using Olbert.LanHistory.Properties;
using Serilog;

namespace Olbert.LanHistory.ViewModel.Deprecated
{
    public class FileHistoryViewModel : ViewModelBase
    {
        private readonly ILogger _logger;
        private string _error;

        public FileHistoryViewModel()
        {
            _logger = new ViewModelLocator().Logger;

            BackupCommand = new RelayCommand( Backup );
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

        public RelayCommand BackupCommand { get; }

        public void Backup()
        {
            try
            {
                using( FileHistoryService fhs = new FileHistoryService() )
                {
                    fhs.Start();
                }

                _logger.Information( "Started backup" );
            }
            catch( Exception e )
            {
                Error = $"Backup failed; message was {e.Message}";
                _logger.Error( e, "Backup failed; message was {Message}" );
            }
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}