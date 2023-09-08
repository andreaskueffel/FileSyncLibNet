using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;

namespace FileSyncLibNet.FileSyncJob
{
    public class FileSyncJobOptions : IFileSyncJobOptions
    {
        public ILogger Logger { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public NetworkCredential Credentials { get; set; }
        public TimeSpan Interval { get; set; } = TimeSpan.Zero;
        public string SearchPattern { get; set; } = "*.*";
        public List<string> Subfolders { get; set; }
        public bool Recursive { get; set; } = true;
        public bool SyncDeleted { get; set; } = false;
        public bool DeleteSourceAfterBackup { get; set; } = false;
        public FileSyncProvider FileSyncProvider { get; set; } = FileSyncProvider.FileIO;

        public FileSyncJobOptions()
        {
            Subfolders = new List<string>();
        }

    }
}
