using FileSyncLibNet.FileSyncJob;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FileSyncLibNet.SyncProviders
{
    internal abstract class ProviderBase : ISyncProvider
    {
        internal ILogger logger => JobOptions.Logger;
        public IFileSyncJobOptions JobOptions { get; set; }

        public abstract void SyncSourceToDest();
    }
}
