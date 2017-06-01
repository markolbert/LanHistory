
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using Microsoft.Win32;

namespace Olbert.LanHistory
{
    /// <summary>
    /// MvvmLight Messenger class used to report what power event (e.g., waking up
    /// from sleep) just occurred
    /// </summary>
    public class PowerModeMessage
    {
        /// <summary>
        /// The power event that just occurred
        /// </summary>
        public PowerModes Mode { get; set; }
    }
}
