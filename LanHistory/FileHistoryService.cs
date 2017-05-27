
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;

namespace Olbert.LanHistory
{
    public sealed class FileHistoryService : IDisposable
    {
        private readonly IntPtr _pipe = IntPtr.Zero;

        public FileHistoryService()
        {
            NativeMethods.FhServiceOpenPipe( true, ref _pipe );
        }

        public void Start( bool lowPriority = true )
        {
            if( !_pipe.Equals(IntPtr.Zero) )
                NativeMethods.FhServiceStartBackup( _pipe, true );
        }

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
