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
        public (FileHistoryInfo fileHI, StringWriter log) GetSystemConfig()
        {
            var fhInfo = new FileHistoryInfo()
            {
                LastBackup = DateTime.Now,
                Interval = TimeSpan.FromMinutes( 60 ),
                IPAddress = IPAddress.Loopback.ToString(),
                IsRemote = true,
                MacAddress = PhysicalAddress.None.ToString(),
                ServerName = "SomeServer",
                WakeUpTime = 3
            };

            StringWriter log = new StringWriter();
            log.WriteLine("generated design-time data");

            return (fhInfo, log);
        }
    }
}