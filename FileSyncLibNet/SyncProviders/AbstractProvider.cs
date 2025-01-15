using FileSyncLibNet.AccessProviders;
using FileSyncLibNet.Commons;
using FileSyncLibNet.FileCleanJob;
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
            if (!(JobOptions is IFileCleanJobOptions) && !(JobOptions is IFileSyncJobOptions))
            {
                throw new ArgumentException("this instance has no information about syncing files or cleaning files, it has type " + JobOptions.GetType().ToString());
            }
            string stateFilename = null;
            if (JobOptions is IFileSyncJobOptions syncJobOptions)
            {
                if (syncJobOptions.SourcePath.StartsWith("scp:"))
                {
                    SourceAccess = new ScpAccessProvider(syncJobOptions.Credentials, JobOptions.Logger, stateFilename: null);
                }
                else
                {
                    SourceAccess = new FileIoAccessProvider(JobOptions.Logger, stateFilename: null);
                }
                SourceAccess.UpdateAccessPath(syncJobOptions.SourcePath);
                stateFilename = null;
            }
            if (JobOptions.DestinationPath.StartsWith("scp:"))
            {
                DestinationAccess = new ScpAccessProvider(JobOptions.Credentials, JobOptions.Logger, stateFilename);
            }
            else
            {
                DestinationAccess = new FileIoAccessProvider(JobOptions.Logger, stateFilename);
            }

        }

        public override void DeleteFiles()
        {
            //Use from file io provider and adapt to use access provider

            if (!(JobOptions is IFileCleanJobOptions jobOptions))
                throw new ArgumentException("this instance has no information about deleting files, it has type " + JobOptions.GetType().ToString());
            try
            {
                string formattedDestinationPath = string.Format(jobOptions.DestinationPath, DateTime.Now);
                DestinationAccess.UpdateAccessPath(formattedDestinationPath);
            }
            catch (Exception ex) { logger?.LogError(ex, "exception formatting destination accesspath with DateTime.Now as 0 arg {A}", jobOptions.DestinationPath); }

            try
            {
                var minimumLastWriteTime = DateTimeOffset.Now - jobOptions.MaxAge;
                var sourceFiles = DestinationAccess.GetFiles(minimumLastWriteTime: minimumLastWriteTime.DateTime,
                                                       pattern: JobOptions.SearchPattern,
                                                       recursive: JobOptions.Recursive,
                                                       subfolders: JobOptions.Subfolders,
                                                       olderFiles: true);
                int fileCount = 0;
                foreach (var fileToDelete in sourceFiles)
                {
                    logger.LogDebug("deleting file {A}", fileToDelete.Name);

                    try
                    {
                        DestinationAccess.Delete(fileToDelete);

                        fileCount++;
                    }
                    catch (Exception ex) { logger.LogError(ex, "exception deleting file {A}", fileToDelete.Name); }
                    logger.LogDebug("deleted {A} files", fileCount);
                }


            }
            catch (Exception exc)
            {
                logger.LogError(exc, "TimerCleanup_Tick threw an exception");
            }
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
            DateTimeOffset minimumLastWriteTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            if (jobOptions.RememberLastSync)
            {
                if (LastRun.ToUnixTimeMilliseconds() == 0)
                    LastRun = jobOptions.MaxAge < jobOptions.Interval ? new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero) : DateTimeOffset.Now - jobOptions.MaxAge;
                minimumLastWriteTime = LastRun - jobOptions.Interval - jobOptions.Interval;
            }
            else
            {
                minimumLastWriteTime = jobOptions.MaxAge < jobOptions.Interval ?
                    DateTimeOffset.MinValue :
                    DateTimeOffset.Now - jobOptions.MaxAge - jobOptions.Interval;
            }
            if (!System.IO.File.Exists("fullsync.done"))
            {
                logger.LogWarning("fullsync.done not found, syncing all files for initial run");
                minimumLastWriteTime = DateTimeOffset.MinValue;
            }

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
