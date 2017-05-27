﻿
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;

namespace Olbert.LanHistory.Model
{
    public interface IDataService
    {
        DateTime GetLastBackup();
        LanHistory GetLanHistory();
    }
}
