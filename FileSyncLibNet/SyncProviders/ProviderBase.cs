using FileSyncLibNet.Commons;
using Microsoft.Extensions.Logging;
using System;

namespace FileSyncLibNet.SyncProviders
{
    internal abstract class ProviderBase : ISyncProvider
    {
        internal ILogger logger => JobOptions.Logger;
        public IFileJobOptions JobOptions { get; }
        internal DateTimeOffset LastRun { get; set; } = DateTimeOffset.FromUnixTimeMilliseconds(0).ToLocalTime();
        public abstract void SyncSourceToDest();
        public abstract void DeleteFiles();
        public ProviderBase(IFileJobOptions jobOptions)
        {
            JobOptions = jobOptions;
        }
    }
}
