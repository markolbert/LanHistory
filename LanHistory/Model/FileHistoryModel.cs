
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;
using System.Net;
using System.Net.NetworkInformation;

namespace Olbert.LanHistory.Model.Deprecated
{
    public class FileHistoryModel
    {
        public DateTime LastBackup { get; set; } = DateTime.MinValue;
        public string ServerName { get; set; }
        public string UNCPath { get; set; }
        public IPAddress IPAddress { get; set; } = IPAddress.None;
        public PhysicalAddress MacAddress { get; set; } = PhysicalAddress.None;
        public string MacAddressText { get; set; }
        public bool IsRemote { get; set; }
    }
}
