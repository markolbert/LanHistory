
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;
using System.Net.NetworkInformation;
using Olbert.LanHistory.ViewModel;

namespace Olbert.LanHistory
{
    /// <summary>
    /// MvvmLight Messenger class used to report information about the configuration
    /// of the Windows File History service
    /// </summary>
    public class LanHistoryMessage
    {
        /// <summary>
        /// Returns the latest information about the Windows File History service, or null
        /// if nothing has changed
        /// </summary>
        /// <returns>the latest information about the Windows File History service, or null
        /// if nothing has changed</returns>
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

        /// <summary>
        /// The date and time the last backup took place
        /// </summary>
        public DateTime LastBackup { get; set; }

        /// <summary>
        /// The UNC path to where Windows File History backs up files
        /// </summary>
        public string UNCPath { get; set; }

        /// <summary>
        /// The MAC address of the server to which Windows File History backs up files
        /// </summary>
        public PhysicalAddress MacAddress { get; set; }
    }

    //public class IntervalChangedMessage
    //{
    //    public TimeSpan Interval { get; set; }
    //}

    //public class WakeUpChangedMessage
    //{
    //    public int WakeUpTime { get; set; }
    //}
}
