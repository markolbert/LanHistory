using System;
using System.IO;
using System.Threading.Tasks;
using Olbert.LanHistory.ViewModel;

namespace Olbert.LanHistory
{
    public class ServerStatusMessage
    {
        public static async Task<ServerStatusMessage> GetChanged( bool priorAccessibility )
        {
            bool curAccessibility = await Task.Run( () => Directory.Exists( new ViewModelLocator().LanHistory.UNCPath ) );

            return curAccessibility == priorAccessibility
                ? null
                : new ServerStatusMessage()
                {
                    ShareAccessible = curAccessibility,
                };
        }

        public bool ShareAccessible { get; set; }
    }
}
