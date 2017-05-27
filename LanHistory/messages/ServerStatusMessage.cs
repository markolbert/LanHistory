
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System.IO;
using System.Threading.Tasks;
using Olbert.LanHistory.ViewModel;

namespace Olbert.LanHistory
{
    public class ServerStatusMessage
    {
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

        public bool ShareAccessible { get; set; }
    }
}
