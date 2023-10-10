using FileSyncLibNet.Commons;
using FileSyncLibNet.SyncProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;

namespace FileSyncLibNet.FileCleanJob
{
    public abstract class FileJobOptionsBase : IFileJobOptions
    {
        public NetworkCredential Credentials { get; set; }
        public string DestinationPath { get; set; }
        public TimeSpan Interval { get; set; } = TimeSpan.Zero;
        public ILogger Logger { get; set; }
        public string SearchPattern { get; set; } = "*.*";
        public List<string> Subfolders { get; set; } = new List<string>();
        public bool Recursive { get; set; } = true;
        public SyncProvider FileSyncProvider { get; set; } = SyncProvider.FileIO;
    }
}
