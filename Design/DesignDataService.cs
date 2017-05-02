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
    }
}