using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Serilog;

namespace LanHistory.Model
{
    public interface IDataService
    {
        FileHistoryInfo GetSystemConfig();
    }
}
