using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LanHistory.ViewModel;
using Microsoft.Practices.ServiceLocation;

namespace LanHistory.Model
{
    public class FileHistoryInfo
    {
        public DateTime LastBackup { get; set; } = DateTime.MinValue;
        public TimeSpan Interval { get; set; } = UpTimeMonitor.DefaultInterval;
        public string ServerName { get; set; }
        public string IPAddress { get; set; }
        public string MacAddress { get; set; }
        public bool IsRemote { get; set; }
        public int WakeUpTime { get; set; } = UpTimeMonitor.DefaultWakeUpMinutes;
    }
}
