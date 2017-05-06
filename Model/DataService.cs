﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Olbert.LanHistory.Model
{
    public class DataService : IDataService
    {
        private readonly ILogger _logger;

        public DataService( ILogger logger )
        {
            _logger = logger;
        }

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

                            Int32 curIPInt = NativeMethods.INetAddress(ipAddress.ToString());

                            try
                            {
                                Int64 macInfo = 0;
                                Int32 length = 6;

                                int res = NativeMethods.SendARP(curIPInt, 0, ref macInfo, ref length);

                                if (macInfo == 0)
                                    _logger.Error("No MAC address was found; is the server running?");
                                else
                                {
                                    retVal.MacAddress = new PhysicalAddress(BitConverter.GetBytes(macInfo));
                                    _logger.LogDebugInformation("found MAC address for backup target");
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