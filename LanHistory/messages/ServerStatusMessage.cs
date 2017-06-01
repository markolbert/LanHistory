
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System.IO;
using System.Threading.Tasks;
using Olbert.LanHistory.ViewModel;

namespace Olbert.LanHistory
{
    /// <summary>
    /// MvvmLight Messenger class used to report whether or not the file share used
    /// by Windows File History is accessible
    /// </summary>
    public class ServerStatusMessage
    {
        /// <summary>
        /// returns the current state of the file share used by Windows File History, or null
        /// if nothing has changed since the last check
        /// </summary>
        /// <param name="priorAccessibility">flag indicating whether or not the last check showed the
        /// file share used by Windows File History was accessible; true means it was, false means it
        /// wasn't, and null means its state was not determined</param>
        /// <returns>the current state of the file share used by Windows File History, or null
        /// if nothing has changed since the last check</returns>
        public static async Task<ServerStatusMessage> GetChanged( bool? priorAccessibility )
        {
            bool curAccessibility = await Task.Run( () => Directory.Exists( new ViewModelLocator().LanHistory.UNCPath ) );

            return priorAccessibility.HasValue && curAccessibility == priorAccessibility
                ? null
                : new ServerStatusMessage()
                {
                    ShareAccessible = curAccessibility,
                };
        }

        /// <summary>
        /// Flag indicating whether or not the file share used by Windows File History is accessible
        /// </summary>
        public bool ShareAccessible { get; set; }
    }
}
