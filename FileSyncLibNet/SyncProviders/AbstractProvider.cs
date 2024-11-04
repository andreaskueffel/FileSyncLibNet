using FileSyncLibNet.AccessProviders;
using FileSyncLibNet.Commons;
using FileSyncLibNet.FileSyncJob;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace FileSyncLibNet.SyncProviders
{
    internal class AbstractProvider : ProviderBase
    {
        IAccessProvider SourceAccess { get; set; }
        IAccessProvider DestinationAccess { get; set; }

        public AbstractProvider(IFileJobOptions jobOptions) : base(jobOptions)
        {
            if (!(JobOptions is IFileSyncJobOptions syncJobOptions))
                throw new ArgumentException("this instance has no information about syncing files, it has type " + JobOptions.GetType().ToString());
            if (syncJobOptions.SourcePath.StartsWith("scp:"))
            {
                SourceAccess = new ScpAccessProvider(syncJobOptions.SourcePath, syncJobOptions.Credentials);
            }
            else
            {
                SourceAccess = new FileIoAccessProvider(syncJobOptions.SourcePath);
            }
            if (syncJobOptions.DestinationPath.StartsWith("scp:"))
            {
                DestinationAccess = new ScpAccessProvider(syncJobOptions.DestinationPath, syncJobOptions.Credentials);
            }
            else
            {
                DestinationAccess = new FileIoAccessProvider(syncJobOptions.DestinationPath);
            }
        }

        public override void DeleteFiles()
        {
            throw new NotImplementedException();
        }

        public override void SyncSourceToDest()
        {
            if (!(JobOptions is IFileSyncJobOptions jobOptions))
                throw new ArgumentException("this instance has no information about syncing files, it has type " + JobOptions.GetType().ToString());
            var sw = Stopwatch.StartNew();
            try
            {
                DestinationAccess.CreateDirectory("");
            }
            catch (Exception ex) { logger?.LogError(ex, "exception creating destination directory {A}", jobOptions.DestinationPath); }
            if (JobOptions.Credentials != null)
            {

            }
            int copied = 0;
            int skipped = 0;
            var minimumLastWriteTime = jobOptions.RememberLastSync ? (LastRun - jobOptions.Interval - jobOptions.Interval) : DateTime.MinValue;
            bool error_occured = false;
            try
            {
                var sourceFiles = SourceAccess.GetFiles(minimumLastWriteTime.DateTime, JobOptions.SearchPattern, recursive: JobOptions.Recursive, subfolders: JobOptions.Subfolders);

                foreach (var sourceFile in sourceFiles)
                {
                    var remoteFile = DestinationAccess.GetFileInfo(sourceFile.Name);
                    bool copy = !remoteFile.Exists || remoteFile.Length != sourceFile.Length || remoteFile.LastWriteTime != sourceFile.LastWriteTime;
                    if (copy)
                    {
                        try
                        {
                            logger.LogDebug("Copy {A}", sourceFile.Name);
                            using (var sourceStream = SourceAccess.GetStream(sourceFile))
                            {
                                DestinationAccess.WriteFile(sourceFile, sourceStream);
                            }
                            //file.copy
                            copied++;
                            if (jobOptions.DeleteSourceAfterBackup)
                            {
                                SourceAccess.Delete(sourceFile);
                            }
                        }
                        catch (Exception exc)
                        {
                            error_occured = true;
                            logger.LogError(exc, "Exception copying {A}", sourceFile);
                        }
                    }
                    else
                    {

                        skipped++;
                        logger.LogTrace("Skip {A}", sourceFile);
                    }

                }
                if (!error_occured)
                {
                    LastRun = DateTimeOffset.Now;
                }
            }
            catch(Exception exc)
            {
                logger.LogError(exc, "Exception in main logic of abstract provider");
            }

            sw.Stop();
            logger.LogInformation("{A} files copied, {B} files skipped in {C}s", copied, skipped, sw.ElapsedMilliseconds / 1000.0);
        }
    }
}
