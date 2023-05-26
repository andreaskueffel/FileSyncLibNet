using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace FileSyncLibNet.FileSyncJob
{
    public interface IFileSyncJobOptions
    {
        NetworkCredential Credentials { get; set; }
        string DestinationPath { get; set; }
        FileSyncProvider FileSyncProvider { get; set; }
        TimeSpan Interval { get; set; }
        ILogger Logger { get; set; }
        bool Recursive { get; set; }
        string SearchPattern { get; set; }
        string SourcePath { get; set; }
        bool SyncDeleted { get; set; }
    }
}