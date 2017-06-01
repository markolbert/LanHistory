
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;

namespace Olbert.LanHistory
{
    /// <summary>
    /// Defines methods for interacting with the Windows File History service
    /// </summary>
    public sealed class FileHistoryService : IDisposable
    {
        private readonly IntPtr _pipe = IntPtr.Zero;

        /// <summary>
        /// Creates an instance of the class, opening a connection to the Windows
        /// File History service.
        /// </summary>
        public FileHistoryService()
        {
            NativeMethods.FhServiceOpenPipe( true, ref _pipe );
        }

        /// <summary>
        /// Starts a backup by the Windows File History service
        /// </summary>
        /// <param name="lowPriority">flag indicating whether or not the backup should
        /// be considered low priority by the system; defaults to true</param>
        public void Start( bool lowPriority = true )
        {
            if( !_pipe.Equals(IntPtr.Zero) )
                NativeMethods.FhServiceStartBackup( _pipe, lowPriority );
        }

        /// <summary>
        /// Stops the current Windows File History service backup
        /// </summary>
        public void Stop()
        {
            if (!_pipe.Equals(IntPtr.Zero))
                NativeMethods.FhServiceStopBackup( _pipe, false );
        }

        private void ReleaseUnmanagedResources()
        {
            if (!_pipe.Equals(IntPtr.Zero))
                NativeMethods.FhServiceClosePipe(_pipe);
        }

        /// <summary>
        /// Disposes of the unmanaged resources used to interface with the Windows
        /// File History service, by closing the connection to it
        /// </summary>
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize( this );
        }

        ~FileHistoryService()
        {
            ReleaseUnmanagedResources();
        }
    }
}
