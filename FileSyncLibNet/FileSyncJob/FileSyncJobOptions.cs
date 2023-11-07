using FileSyncLibNet.FileCleanJob;
using FileSyncLibNet.SyncProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;

namespace FileSyncLibNet.FileSyncJob
{
    public class FileSyncJobOptions : FileJobOptionsBase, IFileSyncJobOptions
    {
        public string SourcePath { get; set; }
        public bool SyncDeleted { get; set; } = false;
        public bool DeleteSourceAfterBackup { get; set; } = false;
        public bool RememberLastSync { get; set; } = true;

        public FileSyncJobOptions()
        {
            
        }

    }
}
