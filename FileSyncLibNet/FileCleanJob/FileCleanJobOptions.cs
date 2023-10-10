using FileSyncLibNet.SyncProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;

namespace FileSyncLibNet.FileCleanJob
{
    public class FileCleanJobOptions : FileJobOptionsBase, IFileCleanJobOptions
    {
        public TimeSpan MaxAge { get; set; }
        public long MinimumFreeSpaceMegabyte { get; set; }
    }
}
