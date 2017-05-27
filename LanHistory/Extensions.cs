
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;
using System.Diagnostics;
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
