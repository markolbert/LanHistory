using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Olbert.LanHistory
{
    public static class Extensions
    {
        [Conditional("DEBUG")]
        public static void LogDebugInformation( this ILogger logger, string mesg )
        {
            if( logger == null )
                throw new NullReferenceException( nameof(logger) );

            logger.Information( mesg );
        }
    }
}
