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
                SourceAccess = new ScpAccessProvider(syncJobOptions.Credentials, jobOptions.Logger, stateFilename: null);
            }
            else
            {
                SourceAccess = new FileIoAccessProvider(jobOptions.Logger, stateFilename: null);
            }
            SourceAccess.UpdateAccessPath(syncJobOptions.SourcePath);
            string stateFilename = syncJobOptions.RememberRemoteState ? syncJobOptions.GetHashedName() : null;
            if (syncJobOptions.DestinationPath.StartsWith("scp:"))
            {
                DestinationAccess = new ScpAccessProvider(syncJobOptions.Credentials, jobOptions.Logger, stateFilename);
            }
            else
            {
                DestinationAccess = new FileIoAccessProvider(jobOptions.Logger, stateFilename);
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
                string formattedDestinationPath = string.Format(jobOptions.DestinationPath, DateTime.Now);
                DestinationAccess.UpdateAccessPath(formattedDestinationPath);
            }
            catch (Exception ex) { logger?.LogError(ex, "exception formatting destination accesspath with DateTime.Now as 0 arg {A}", jobOptions.DestinationPath); }

            bool createDestinationDir = true;
            int copied = 0;
            int skipped = 0;
            var minimumLastWriteTime = jobOptions.RememberLastSync ?
                (LastRun - jobOptions.Interval - jobOptions.Interval) :
                (jobOptions.MaxAge < jobOptions.Interval ?
                    DateTimeOffset.MinValue :
                    DateTimeOffset.Now - jobOptions.MaxAge
                );

            bool error_occured = false;
            try
            {
                var sourceFiles = SourceAccess.GetFiles(minimumLastWriteTime: minimumLastWriteTime.DateTime,
                                                        pattern: JobOptions.SearchPattern,
                                                        recursive: JobOptions.Recursive,
                                                        subfolders: JobOptions.Subfolders);
                foreach (var sourceFile in sourceFiles)
                {
                    var remoteFile = DestinationAccess.GetFileInfo(sourceFile.Name);
                    bool copy = !remoteFile.Exists || remoteFile.Length != sourceFile.Length || remoteFile.LastWriteTime != sourceFile.LastWriteTime;
                    if (copy)
                    {
                        if (createDestinationDir)
                        {
                            try
                            {
                                DestinationAccess.CreateDirectory("");
                            }
                            catch (Exception ex) { logger?.LogError(ex, "exception creating destination directory {A}", jobOptions.DestinationPath); }
                            createDestinationDir = false;
                        }
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
            catch (Exception exc)
            {
                logger.LogError(exc, "Exception in main logic of abstract provider");
            }

            sw.Stop();
            logger.LogInformation("{A} files copied, {B} files skipped in {C}s", copied, skipped, sw.ElapsedMilliseconds / 1000.0);
        }
    }
}
