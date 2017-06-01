
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;

namespace Olbert.LanHistory.Model
{
    /// <summary>
    /// Defines the service used to retrieve the date and time of the last backup, and the Windows File History /
    /// Lan History Manager configuration
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// Gets the date and time of the last backup, or DateTime.MinValue if none has taken place yet
        /// </summary>
        /// <returns>the date and time of the last backup, or DateTime.MinValue if none has taken place yet</returns>
        DateTime GetLastBackup();

        /// <summary>
        /// Gets the current Windows File History / Lan History Manager configuration
        /// </summary>
        /// <returns>the current Windows File History / Lan History Manager configuration</returns>
        LanHistory GetLanHistory();
    }
}
