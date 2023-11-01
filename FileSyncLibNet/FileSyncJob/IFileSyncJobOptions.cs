using FileSyncLibNet.Commons;
using FileSyncLibNet.SyncProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;

namespace FileSyncLibNet.FileSyncJob
{
    public interface IFileSyncJobOptions: IFileJobOptions
    {
        string SourcePath { get; set; }
        bool DeleteSourceAfterBackup { get; set; }
        bool SyncDeleted { get; set; }
    }
}