using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using Olbert.LanHistory.Model;
using Serilog;

namespace Olbert.LanHistory.Design
{
    public class DesignDataService : IDataService
    {
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

        public DateTime GetLastBackup()
        {
            return DateTime.Now;
        }

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