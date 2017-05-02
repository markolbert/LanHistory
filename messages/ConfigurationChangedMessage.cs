using System;
using System.Net.NetworkInformation;

namespace Olbert.LanHistory
{
    public class ConfigurationChangedMessage
    {
        public PhysicalAddress MacAddress { get; set; }
        public TimeSpan Interval { get; set; }
        public int WakeUpTime { get; set; }
    }
}
