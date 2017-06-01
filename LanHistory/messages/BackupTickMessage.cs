
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;

namespace Olbert.LanHistory
{
    /// <summary>
    /// MvvmLight Messenger class used to report how much time is left until
    /// the next backup is triggered
    /// </summary>
    public class BackupTickMessage
    {
        /// <summary>
        /// The time remaining until the next backup is triggered
        /// </summary>
        public TimeSpan TimeRemaining { get; set; }
    }
}
