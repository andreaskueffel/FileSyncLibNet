using FileSyncLibNet.Commons;
using FileSyncLibNet.SyncProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;

namespace FileSyncLibNet.FileCleanJob
{
    public interface IFileCleanJobOptions :IFileJobOptions
    {
        TimeSpan MaxAge { get; set; }
        long MinimumFreeSpaceMegabyte { get; set; } 
    }
}
