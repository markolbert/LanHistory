
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Xml.Linq;
using Microsoft.Win32;
using Serilog;

namespace Olbert.LanHistory.Model
{
    /// <summary>
    /// Run time implementation of IDataService. Used to retrieve information about the last backup, and
    /// the current configuration of Windows File History, which can change
    /// </summary>
    public class DataService : IDataService
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Creates an instance of DataService
        /// </summary>
        /// <param name="logger">an instance of a Serilog logger; a NullReferenceException is thrown if this
        /// is undefined</param>
        public DataService( ILogger logger )
        {
            _logger = logger ?? throw new NullReferenceException( nameof(logger) );
        }

        /// <summary>
        /// Gets the date and time of the last backup
        /// </summary>
        /// <returns>the date and time fo the last backup; returns DateTime.MinValue if no
        /// backup has yet taken place</returns>
        public DateTime GetLastBackup()
        {
            DateTime? retVal = null;

            using (RegistryKey reg = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
            {
                using (RegistryKey key = reg.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\FileHistory"))
                {
                    long lastBackup = (long?)key?.GetValue("ProtectedUpToTime") ?? 0;

                    if (lastBackup <= 0)
                        _logger.Error($"Could not find registry key {RegistryHive.CurrentUser.ToString()}\\Software\\Microsoft\\Windows\\CurrentVersion\\FileHistory");
                    else
                    {
                        _logger.LogDebugInformation("retrieved last backup time");
                        retVal = DateTime.FromFileTime(lastBackup).ToLocalTime();
                    }
                }
            }

            return retVal ?? DateTime.MinValue;
        }

        /// <summary>
        /// Gets the latest configuration information for Windows File History and Lan History Manager
        /// </summary>
        /// <returns>the latest configuration information for Windows File History and Lan History Manager, or null
        /// if the information cannot be retrieved</returns>
        public LanHistory GetLanHistory()
        {
            LanHistory retVal = null;
            bool configValid = true;

            using (RegistryKey reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
            {
                using (RegistryKey key =
                    reg.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\fhsvc\Parameters\Configs"))
                {
                    string configPath = key?.GetValueNames().First();

                    if (configPath == null)
                    {
                        _logger.Error("Could not locate configuration files");
                        return null;
                    }

                    configPath += "1.xml";

                    if (!File.Exists(configPath))
                    {
                        _logger.Error($"Could not locate configuration file {configPath}");
                        return null;
                    }

                    XDocument doc = null;
                    retVal = new LanHistory();

                    try
                    {
                        doc = XDocument.Parse(File.ReadAllText(configPath));
                        _logger.LogDebugInformation("parsed File History configuration file");
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Failed to parse File History configuration file, message was {e.Message}");
                        return null;
                    }

                    XElement target = doc.Descendants("Target").FirstOrDefault();
                    if (target == null)
                    {
                        _logger.Error("Could not locate ServerName information in File History configuration file");
                        return null;
                    }
                    else _logger.LogDebugInformation("found ServerName element in File History configuration file");

                    XElement driveType = target.Descendants("TargetDriveType").FirstOrDefault();
                    if (driveType == null)
                    {
                        _logger.Error("Could not find TargetDriveType in File History configuration file");
                        configValid = false;
                    }
                    else
                    {
                        _logger.LogDebugInformation("found TargetDriveType element in File History configuration file");
                        retVal.IsRemote = driveType.Value.Equals("remote", StringComparison.OrdinalIgnoreCase);
                    }

                    XElement url = target.Element("TargetUrl");

                    retVal.UNCPath = url?.Value;
                    string[] parts = null;

                    if (String.IsNullOrEmpty(retVal.UNCPath))
                    {
                        _logger.Error("Could not find TargetUrl in File History configuration file");
                        configValid = false;
                    }
                    else
                    {
                        _logger.LogDebugInformation("found TargetUrl element in File History configuration file");
                        parts = retVal.UNCPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                        configValid = parts.Length > 0;
                    }

                    if (configValid)
                    {
                        var ipAddress = Dns.GetHostAddresses(parts[0]).FirstOrDefault();

                        if (ipAddress == null)
                        {
                            _logger.Error("Could not get IP address for backup target");
                            configValid = false;
                        }
                        else
                        {
                            _logger.LogDebugInformation("found IP address for backup target");

                            Int32 curIPInt = BitConverter.ToInt32( ipAddress.GetAddressBytes(), 0 );

                            try
                            {
                                Int64 macInfo = 0;
                                Int32 length = 6;

                                int res = NativeMethods.SendARP(curIPInt, 0, ref macInfo, ref length);

                                if( res != 0 )
                                    _logger.Error( $"NativeMethods.SendArp() returned value {res}" );

                                if ( macInfo == 0 )
                                    _logger.Error(
                                        $"No MAC address was found for IP address {ipAddress} ({curIPInt}) ; is the server running?" );
                                else
                                {
                                    retVal.MacAddress = new PhysicalAddress( BitConverter.GetBytes( macInfo ) );
                                    _logger.LogDebugInformation( "found MAC address for backup target" );
                                }
                            }
                            catch (Exception e)
                            {
                                configValid = false;
                                _logger.Error($"Failed to find MAC address for backup target, message was {e.Message}");
                            }
                        }
                    }
                }

            }

            return configValid ? retVal : null;
        }
    }
}