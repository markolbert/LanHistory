using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Xml.Linq;
using Microsoft.Win32;
using Serilog;

namespace LanHistory.Model
{
    public class DataService : IDataService
    {
        [DllImport("Iphlpapi.dll")]
        private static extern int SendARP(Int32 dest, Int32 host, ref Int64 mac, ref Int32 len);

        [DllImport("Ws2_32.dll")]
        private static extern Int32 inet_addr(string ip);

        public (FileHistoryInfo fileHI, StringWriter log) GetSystemConfig()
        {
            var log = new StringWriter();

            bool configValid = true;
            FileHistoryInfo retVal = new FileHistoryInfo();

            using( RegistryKey reg = RegistryKey.OpenBaseKey( RegistryHive.CurrentUser, RegistryView.Default ) )
            {
                using( RegistryKey key = reg.OpenSubKey( @"Software\Microsoft\Windows\CurrentVersion\FileHistory" ) )
                {
                    long lastBackup = (long?) key?.GetValue( "ProtectedUpToTime" ) ?? 0;

                    if( lastBackup <= 0 )
                    {
                        log.WriteLine($"Could not find registry key {RegistryHive.CurrentUser.ToString()}\\Software\\Microsoft\\Windows\\CurrentVersion\\FileHistory" );
                        configValid = false;
                    }
                    else
                    {
                        log.WriteLine( "retrieved last backup time" );
                        retVal.LastBackup = DateTime.FromFileTime( lastBackup ).ToLocalTime();
                    }
                }
            }

            using( RegistryKey reg = RegistryKey.OpenBaseKey( RegistryHive.LocalMachine, RegistryView.Default ) )
            {
                using( RegistryKey key =
                    reg.OpenSubKey( @"SYSTEM\CurrentControlSet\Services\fhsvc\Parameters\Configs" ) )
                {
                    string configPath = key?.GetValueNames().First();

                    if( configPath == null )
                    {
                        log.WriteLine( "Could not locate configuration files" );
                        return (null, log);
                    }

                    configPath += "1.xml";

                    if( !File.Exists( configPath ) )
                    {
                        log.WriteLine( $"Could not locate configuration file {configPath}" );
                        return (null, log);
                    }

                    XDocument doc = null;

                    try
                    {
                        doc = XDocument.Parse( File.ReadAllText( configPath ) );
                        log.WriteLine( "parsed File History configuration file" );
                    }
                    catch( Exception e )
                    {
                        log.WriteLine( $"Failed to parse File History configuration file, message was {e.Message}" );
                        return (null, log);
                    }

                    XElement target = doc.Descendants( "Target" ).FirstOrDefault();
                    if( target == null )
                    {
                        log.WriteLine( "Could not locate ServerName information in File History configuration file" );
                        return (null, log);
                    }
                    else log.WriteLine( "found ServerName element in File History configuration file" );

                    XElement driveType = target.Descendants( "TargetDriveType" ).FirstOrDefault();
                    if( driveType == null )
                    {
                        log.WriteLine( "Could not find TargetDriveType in File History configuration file" );
                        configValid = false;
                    }
                    else
                    {
                        log.WriteLine( "found TargetDriveType element in File History configuration file" );
                        retVal.IsRemote = driveType.Value.Equals( "remote", StringComparison.OrdinalIgnoreCase );
                    }

                    XElement frequency = doc.Descendants( "DPFrequency" ).FirstOrDefault();
                    if( frequency == null )
                    {
                        log.WriteLine( "Could not find backup frequency in File History configuration file" );
                        configValid = false;
                    }
                    else
                    {
                        log.WriteLine( "found backup frequency element in File History configuration file" );
                        retVal.Interval = TimeSpan.FromSeconds( Convert.ToInt32( frequency.Value ) );
                    }

                    XElement url = target.Element( "TargetUrl" );

                    string unc = url?.Value;
                    string[] parts = null;

                    if( String.IsNullOrEmpty( unc ) )
                    {
                        log.WriteLine( "Could not find TargetUrl in File History configuration file" );
                        configValid = false;
                    }
                    else
                    {
                        log.WriteLine( "found TargetUrl element in File History configuration file" );
                        parts = unc.Split( new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries );
                        configValid = parts.Length > 0;
                    }

                    if( configValid )
                    {
                        retVal.ServerName = parts[ 0 ];
                        var ipAddress = Dns.GetHostAddresses( parts[ 0 ] ).FirstOrDefault();

                        if( ipAddress == null )
                        {
                            log.WriteLine( "Could not get IP address for backup target" );
                            configValid = false;
                        }
                        else
                        {
                            retVal.IPAddress = ipAddress.ToString();
                            log.WriteLine( "found IP address for backup target" );

                            Int32 curIPInt = inet_addr( retVal.IPAddress.ToString() );

                            try
                            {
                                Int64 macInfo = 0;
                                Int32 length = 6;

                                int res = SendARP( curIPInt, 0, ref macInfo, ref length );
                                retVal.MacAddress = string.Join( ":", BitConverter.GetBytes( macInfo )
                                    .Where( ( x, i ) => i < 6 )
                                    .Select( z => z.ToString( "X2" ) ) );

                                log.WriteLine( "found MAC address for backup target" );
                            }
                            catch( Exception e )
                            {
                                configValid = false;
                                log.WriteLine( $"Failed to find MAC address for backup target, message was {e.Message}" );
                            }
                        }
                    }
                }

            }

            return configValid ? (retVal, log) : (null, log);
        }

        [Obsolete]
        private List<IPAddress> PingSweep( string targetName )
        {
            // search over each of our active IP addresses
            Ping ping = new Ping();
            PingReply pingReply = null;
            List<IPAddress> retVal = new List<IPAddress>();

            foreach( var adapter in NetworkInterface.GetAllNetworkInterfaces()
                .Where( x => x.OperationalStatus == OperationalStatus.Up &&
                             ( x.NetworkInterfaceType == NetworkInterfaceType.Ethernet || x.NetworkInterfaceType ==
                               NetworkInterfaceType.GigabitEthernet ) ) )
            {
                foreach( UnicastIPAddressInformation unicast in adapter.GetIPProperties()
                    .UnicastAddresses.Where( x => x.Address.AddressFamily == AddressFamily.InterNetwork ) )
                {
                    uint mask = BitConverter.ToUInt32( unicast.IPv4Mask.GetAddressBytes().Reverse().ToArray(), 0 );
                    uint ipSelf = BitConverter.ToUInt32( unicast.Address.GetAddressBytes().Reverse().ToArray(), 0 );

                    uint first = ipSelf & mask;
                    uint last = first | ( 0xffffffff & ~mask );

                    for( uint curAddress = first; curAddress <= last; curAddress++ )
                    {
                        byte[] curBytes = BitConverter.GetBytes( curAddress );
                        var ipAddress = new IPAddress( curBytes.Reverse().ToArray() );

                        for( var idx = 0; idx < 3; idx++ )
                        {
                            try
                            {
                                pingReply = ping.Send( ipAddress, 3000 );
                                if( pingReply != null && pingReply.Status == IPStatus.Success )
                                {
                                    retVal.Add(ipAddress);
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                   
                }
            }

            return retVal;
        }
    }
}