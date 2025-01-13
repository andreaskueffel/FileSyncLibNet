using FileSyncLibNet.Commons;
using FileSyncLibNet.SyncProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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

        public virtual string GetHashedName()
        {
            string readableInfo = $"{Path.GetFileName(DestinationPath.TrimEnd(Path.DirectorySeparatorChar))}_{Interval.TotalMinutes}min";
            string allProperties = $"{DestinationPath}_{SearchPattern}_{Interval}_{Recursive}_{string.Join(",", Subfolders)}";
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
