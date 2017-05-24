using System;
using System.Net.NetworkInformation;
using Olbert.LanHistory.Model;
using Olbert.LanHistory.Properties;
using Olbert.LanHistory.ViewModel;

namespace Olbert.LanHistory
{
    public class LanHistoryMessage
    {
        public static LanHistoryMessage GetChanged()
        {
            var vml = new ViewModelLocator();

            var curLH = vml.DataService.GetLanHistory();
            var priorLH = vml.LanHistory;

            LanHistoryMessage retVal = new LanHistoryMessage()
            {
                LastBackup = curLH.LastBackup,
                MacAddress = curLH.MacAddress,
                UNCPath = curLH.UNCPath,
            };

            bool hasChanges = priorLH != null
                              && ( !retVal.LastBackup.Equals( priorLH.LastBackup )
                                   || !retVal.MacAddress.Equals( priorLH.MacAddress )
                                   || !retVal.UNCPath.Equals( priorLH.UNCPath, StringComparison.OrdinalIgnoreCase ) );

            return hasChanges ? retVal : null;
        }

        public DateTime LastBackup { get; set; }
        public string UNCPath { get; set; }
        public PhysicalAddress MacAddress { get; set; }
    }

    public class IntervalChangedMessage
    {
        public TimeSpan Interval { get; set; }
    }

    public class WakeUpChangedMessage
    {
        public int WakeUpTime { get; set; }
    }
}
