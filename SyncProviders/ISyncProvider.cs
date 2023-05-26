using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using FileSyncLibNet.FileSyncJob;

namespace FileSyncLibNet.SyncProviders
{
    internal interface ISyncProvider
    {
        IFileSyncJobOptions JobOptions { get; set; }
        void SyncSourceToDest();

    }
}
