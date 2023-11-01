using FileSyncLibNet.Commons;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FileSyncLibNet.FileCleanJob
{
    public class FileCleanJob // : IFileJob
    {


        private readonly Timer TimerCleanup;
        private readonly ILogger log;
        private FileCleanJob(IFileCleanJobOptions fileCleanJobOptions)
        {
            log = fileCleanJobOptions.Logger;
            //TimerCleanup = new Timer(new TimerCallback(CleanUp), null, TimeSpan.FromSeconds(20), fileCleanJobOptions.Interval);
            log.LogInformation("Creating timer for cleanup with interval {A}", fileCleanJobOptions.Interval);

        }

        public static IFileJob CreateJob(IFileCleanJobOptions fileCleanJobOptions)
        {
            return (IFileJob)new FileCleanJob(fileCleanJobOptions);
        }

        //void CleanUp(object state)
        //{
        //    try
        //    {
        //        Dictionary<string, int> pathMaxDays = new Dictionary<string, int>();
        //        string[] subFolders = { "HriFFTLog", "HriShockLog", "HriLog", "HriDebugLog", "raw" };
        //        var driveInfo = new DriveInfo(Hauptprogramms.First().ProductionDataPath);
        //        int days = 59;

        //        foreach (Hauptprogramm h in Hauptprogramms)
        //        {
        //            int maxDays = Math.Max(0, h.Einstellungen.BasicSettings["DeleteProductionDataAfterDays"] - 1);
        //            var paths = subFolders.Select(x => Path.Combine(h.ProductionDataPath, x));
        //            foreach (var p in paths)
        //                pathMaxDays.Add(p, maxDays);
        //        }
        //        long minimumFreeSpace = Math.Max(2048, Hauptprogramms.Max(x => (int)x.Einstellungen.BasicSettings["MinimumFreeSpace"])) * 1024L * 1024L;

        //        int fileCount = 0;
        //        int hours = 24;
        //        bool lowSpace = false;
        //        while ((lowSpace = (driveInfo.AvailableFreeSpace < minimumFreeSpace && hours > 1)) || pathMaxDays.Values.Where(x => x <= days).Any())
        //        {
        //            List<FileInfo> filesToDelete = new List<FileInfo>();
        //            var timeDiff = new TimeSpan(days, hours, 0, 0, 0);
        //            foreach (var pathMaxDay in pathMaxDays)
        //            {
        //                if (!lowSpace && pathMaxDay.Value > days)
        //                    continue;
        //                try
        //                {
        //                    if (!Directory.Exists(pathMaxDay.Key))
        //                        Directory.CreateDirectory(pathMaxDay.Key);
        //                    DirectoryInfo di = new DirectoryInfo(pathMaxDay.Key);
        //                    FileInfo[] fi = di.GetFiles();
        //                    foreach (FileInfo f in fi)
        //                    {
        //                        if (f.LastWriteTime < (DateTime.Now - timeDiff))
        //                        {
        //                            filesToDelete.Add(f);
        //                        }
        //                    }
        //                }
        //                catch (Exception ex) { log.LogError(ex, "exception getting file information of {A}", pathMaxDay.Key); }
        //            }
        //            if (filesToDelete.Count > 0)
        //                log.LogWarning("free space on drive {A} {B} MB is below limit of {C} MB. Deleting {D} files older than {E}", driveInfo.RootDirectory, (driveInfo.AvailableFreeSpace / 1024L / 1024L), (minimumFreeSpace / 1024L / 1024L), filesToDelete.Count, timeDiff);
        //            //else
        //            //    log.LogDebug("free space on drive {A} {B} MB is below limit of {C} MB. No files older than {D} found", driveInfo.RootDirectory, (driveInfo.AvailableFreeSpace / 1024L / 1024L), (minimumFreeSpace / 1024L / 1024L), timeDiff);
        //            foreach (var file in filesToDelete)
        //            {
        //                log.LogDebug("deleting file {A}", file.FullName);

        //                try
        //                {
        //                    file.Delete();
        //                    fileCount++;
        //                }
        //                catch (Exception ex) { log.LogError(ex, "exception deleting file {A}", file.FullName); }
        //            }
        //            if (days > 0)
        //                days--;
        //            else
        //                hours--;

        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        log.LogError(exc, "TimerCleanup_Tick threw an exception");
        //    }

        //}

    }
}
