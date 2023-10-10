using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using FileSyncLibNet.Commons;
using FileSyncLibNet.FileSyncJob;

namespace FileSyncLibNet.SyncProviders
{
    internal interface ISyncProvider
    {
        IFileJobOptions JobOptions { get; }
        void SyncSourceToDest();
        void DeleteFiles();

    }
}
