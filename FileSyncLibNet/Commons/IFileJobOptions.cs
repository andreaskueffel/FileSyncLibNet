using FileSyncLibNet.SyncProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FileSyncLibNet.Commons
{
    public interface IFileJobOptions
    {
        NetworkCredential Credentials { get; set; }
        string DestinationPath { get; set; }
        SyncProvider FileSyncProvider { get; set; }
        TimeSpan Interval { get; set; }
        ILogger Logger { get; set; }
        bool Recursive { get; set; }
        string SearchPattern { get; set; }
        List<string> Subfolders { get; set; }
        string GetHashedName();
    }
}
