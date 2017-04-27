using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using LanHistory.Model;
using Serilog;

namespace LanHistory.Design
{
    public class DesignDataService : IDataService
    {
        public FileHistoryInfo GetSystemConfig()
        {
            return new FileHistoryInfo()
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