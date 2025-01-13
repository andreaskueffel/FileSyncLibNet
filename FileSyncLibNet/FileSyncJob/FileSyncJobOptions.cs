using FileSyncLibNet.FileCleanJob;
using FileSyncLibNet.SyncProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace FileSyncLibNet.FileSyncJob
{
    public class FileSyncJobOptions : FileJobOptionsBase, IFileSyncJobOptions
    {
        public string SourcePath { get; set; }
        public bool SyncDeleted { get; set; } = false;
        public bool DeleteSourceAfterBackup { get; set; } = false;
        public bool RememberLastSync { get; set; } = true;
        public bool RememberRemoteState { get; set; } = false;
        public TimeSpan MaxAge { get; set; }

        public FileSyncJobOptions()
        {
            
        }
        public override string GetHashedName()
        {
            string readableInfo = $"{Path.GetFileName(SourcePath.TrimEnd(Path.DirectorySeparatorChar))}_{Path.GetFileName(DestinationPath.TrimEnd(Path.DirectorySeparatorChar))}_{Interval.TotalMinutes}min";
            string allProperties = $"{SourcePath}_{DestinationPath}_{SearchPattern}_{Interval}_{Recursive}_{string.Join(",", Subfolders)}";
            string hash;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(allProperties);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
            return $"{readableInfo}_{hash}";
        }
    }
}
