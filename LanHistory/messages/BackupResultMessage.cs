
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

namespace Olbert.LanHistory
{
    /// <summary>
    /// MvvmLight Messenger class used to report whether or not the last backup
    /// job request succeeded
    /// </summary>
    public class BackupResultMessage
    {
        /// <summary>
        /// Flag indicating whether or not the last backup job request succeeded
        /// </summary>
        public bool Succeeded { get; set; }
    }
}
