using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Olbert.LanHistory
{
    public class FileHistoryService : IDisposable
    {
        [DllImport("FhSvcCtl.dll")]
        private static extern int FhServiceClosePipe([In] IntPtr pipe);

        [DllImport("FhSvcCtl.dll")]
        private static extern int FhServiceOpenPipe([In] bool startServiceIfStopped, ref IntPtr pipe);

        [DllImport("FhSvcCtl.dll")]
        private static extern int FhServiceReloadConfiguration([In] ref IntPtr pipe);

        [DllImport("FhSvcCtl.dll")]
        private static extern int FhServiceBlockBackup([In] ref IntPtr pipe);

        [DllImport("FhSvcCtl.dll")]
        private static extern int FhServiceStartBackup([In] IntPtr pipe, [In] bool lowPriorityIo);

        [DllImport("FhSvcCtl.dll")]
        private static extern int FhServiceStopBackup([In] IntPtr pipe, [In] bool stopTracking);

        [DllImport("FhSvcCtl.dll")]
        private static extern int FhServiceUnblockBackup([In] IntPtr pipe);

        private readonly IntPtr _pipe = IntPtr.Zero;

        public FileHistoryService()
        {
            FhServiceOpenPipe( true, ref _pipe );
        }

        public void Start( bool lowPriority = true )
        {
            if( !_pipe.Equals(IntPtr.Zero) )
                FhServiceStartBackup( _pipe, true );
        }

        public void Stop()
        {
            if (!_pipe.Equals(IntPtr.Zero))
                FhServiceStopBackup( _pipe, false );
        }

        private void ReleaseUnmanagedResources()
        {
            if (!_pipe.Equals(IntPtr.Zero))
                FhServiceClosePipe( _pipe );
        }

        protected virtual void Dispose( bool disposing )
        {
            ReleaseUnmanagedResources();

            if( disposing )
            {
            }
        }

        public void Dispose()
        {
            Dispose( true );

            GC.SuppressFinalize( this );
        }

        ~FileHistoryService()
        {
            Dispose( false );
        }
    }
}
