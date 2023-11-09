using FileSyncLibNet.Commons;

namespace FileSyncLibNet.FileSyncJob
{
    public interface IFileSyncJobOptions : IFileJobOptions
    {
        string SourcePath { get; set; }
        bool DeleteSourceAfterBackup { get; set; }
        bool SyncDeleted { get; set; }
        bool RememberLastSync { get; set; }
    }
}