using System;
using System.Runtime.InteropServices;

namespace Olbert.LanHistory
{
    internal class NativeMethods : IDisposable
    {
        [ DllImport( "FhSvcCtl.dll" ) ]
        internal static extern int FhServiceClosePipe( [ In ] IntPtr pipe );

        [ DllImport( "FhSvcCtl.dll" ) ]
        internal static extern int FhServiceOpenPipe( [ In ] bool startServiceIfStopped, ref IntPtr pipe );

        [ DllImport( "FhSvcCtl.dll" ) ]
        internal static extern int FhServiceReloadConfiguration( [ In ] ref IntPtr pipe );

        [ DllImport( "FhSvcCtl.dll" ) ]
        internal static extern int FhServiceBlockBackup( [ In ] ref IntPtr pipe );

        [ DllImport( "FhSvcCtl.dll" ) ]
        internal static extern int FhServiceStartBackup( [ In ] IntPtr pipe, [ In ] bool lowPriorityIo );

        [ DllImport( "FhSvcCtl.dll" ) ]
        internal static extern int FhServiceStopBackup( [ In ] IntPtr pipe, [ In ] bool stopTracking );

        [ DllImport( "FhSvcCtl.dll" ) ]
        internal static extern int FhServiceUnblockBackup( [ In ] IntPtr pipe );

        [ DllImport( "Iphlpapi.dll" ) ]
        internal static extern int SendARP( Int32 dest, Int32 host, ref Int64 mac, ref Int32 len );

        [ DllImport( "Ws2_32.dll", CharSet = CharSet.Unicode, EntryPoint = "inet_addr" ) ]
        internal static extern Int32 INetAddress( string ip );

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize( this );
        }

        ~NativeMethods()
        {
            ReleaseUnmanagedResources();
        }
    }
}