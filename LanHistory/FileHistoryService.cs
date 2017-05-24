using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
