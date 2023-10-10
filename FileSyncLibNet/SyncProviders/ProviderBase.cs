using FileSyncLibNet.Commons;
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
        public IFileJobOptions JobOptions { get; }

        public abstract void SyncSourceToDest();
        public abstract void DeleteFiles();
        public ProviderBase(IFileJobOptions jobOptions)
        {
            JobOptions = jobOptions;
        }
    }
}
