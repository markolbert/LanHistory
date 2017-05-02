using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Serilog;

namespace Olbert.LanHistory.Model
{
    public interface IDataService
    {
        FileHistoryModel GetSystemConfig();
    }
}
