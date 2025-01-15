using FileSyncLibNet.Commons;
using System;

namespace FileSyncLibNet.FileSyncJob
{
    public interface IFileSyncJobOptions : IFileJobOptions
    {
        string SourcePath { get; set; }
        bool DeleteSourceAfterBackup { get; set; }
        bool SyncDeleted { get; set; }
        bool RememberLastSync { get; set; }
        TimeSpan MaxAge { get; set; }
    }
}