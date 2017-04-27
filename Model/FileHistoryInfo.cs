using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using LanHistory.ViewModel;
using Microsoft.Practices.ServiceLocation;

namespace LanHistory.Model
{
    public class FileHistoryInfo
    {
        public DateTime LastBackup { get; set; } = DateTime.MinValue;
        public string ServerName { get; set; }
        public IPAddress IPAddress { get; set; } = IPAddress.None;
        public PhysicalAddress MacAddress { get; set; } = PhysicalAddress.None;
        public string MacAddressText { get; set; }
        public bool IsRemote { get; set; }
    }
}
