using System;
using System.Net;
using System.Net.NetworkInformation;
using Olbert.LanHistory.Model;

namespace Olbert.LanHistory.Design
{
    /// <summary>
    /// Design time implementation of IDataService. Not used, but having it avoids some annoying error messages
    /// in the designer.
    /// </summary>
    public class DesignDataService : IDataService
    {
        /// <summary>
        /// Gets dummy system information at design time
        /// </summary>
        /// <returns>dummy system information</returns>
        public FileHistoryModel GetSystemConfig()
        {
            return new FileHistoryModel()
            {
                LastBackup = DateTime.Now,
                IPAddress = IPAddress.Loopback,
                IsRemote = true,
                MacAddress = PhysicalAddress.None,
                MacAddressText = PhysicalAddress.None.ToString(),
                ServerName = "SomeServer"
            };
        }

        /// <summary>
        /// Gets the date and time of the last backup done by Windows File History.
        /// 
        /// In design mode, always returns the current date and time
        /// </summary>
        /// <returns></returns>
        public DateTime GetLastBackup()
        {
            return DateTime.Now;
        }

        /// <summary>
        /// Gets dummy information about the network file share used by Windows File History
        /// during design time
        /// </summary>
        /// <returns></returns>
        public Model.LanHistory GetLanHistory()
        {
            return new Model.LanHistory()
            {
                IsRemote = true,
                MacAddress = PhysicalAddress.None,
                MACAddressText = PhysicalAddress.None.ToString(),
            };
        }
    }
}