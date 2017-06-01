
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;
using System.Diagnostics;
using Serilog;

namespace Olbert.LanHistory
{
    /// <summary>
    /// Utility extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Logs a message to the Serilog logger, but only when the application is running in
        /// debug mode
        /// </summary>
        /// <param name="logger">the Serilog logger</param>
        /// <param name="mesg">the message to log</param>
        [Conditional("DEBUG")]
        public static void LogDebugInformation( this ILogger logger, string mesg )
        {
            if( logger == null )
                throw new NullReferenceException( nameof(logger) );

            logger.Information( mesg );
        }
    }
}
