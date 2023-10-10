using FileSyncLibNet.Commons;
using FileSyncLibNet.FileCleanJob;
using FileSyncLibNet.FileSyncJob;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace FileSyncLibNet.SyncProviders
{
    internal class FileIOProvider : ProviderBase
    {
        public FileIOProvider(IFileJobOptions options):base(options)
        {
            
        }

        public override void SyncSourceToDest()
        {
            if (!(JobOptions is IFileSyncJobOptions jobOptions))
                throw new ArgumentException("this instance has no information about syncing files, it has type " + JobOptions.GetType().ToString());
            var sw = Stopwatch.StartNew();
            Directory.CreateDirectory(jobOptions.SourcePath);
            Directory.CreateDirectory(JobOptions.DestinationPath);
            int copied = 0;
            int skipped = 0;
            DirectoryInfo _di = new DirectoryInfo(jobOptions.SourcePath);
            //Dateien ins Backup kopieren
            if (JobOptions.Credentials != null)
            {

            }
            
            
            foreach (var dir in JobOptions.Subfolders.Count > 0 ? _di.GetDirectories() : new[] {_di})
            {
                if (JobOptions.Subfolders.Count > 0 && !JobOptions.Subfolders.Select(x => x.ToLower()).Contains(dir.Name.ToLower()))
                    continue;
                var _fi = dir.EnumerateFiles(
                    searchPattern: JobOptions.SearchPattern,
                    searchOption: JobOptions.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);


                foreach (FileInfo f in _fi)
                {

                    var relativeFilename = f.FullName.Substring(Path.GetFullPath(jobOptions.SourcePath).Length).TrimStart('\\');
                    var remotefile = new FileInfo(Path.Combine(JobOptions.DestinationPath, relativeFilename));
                    bool copy = !remotefile.Exists || remotefile.Length != f.Length || remotefile.LastWriteTime != f.LastWriteTime;
                    if (copy)
                    {
                        try
                        {
                            logger.LogDebug("Copy {A}", relativeFilename);
                            File.Copy(f.FullName, remotefile.FullName, true);
                            copied++;
                            if (jobOptions.DeleteSourceAfterBackup)
                            {
                                File.Delete(f.FullName);
                            }
                        }
                        catch (Exception exc)
                        {
                            logger.LogError(exc, "Exception copying {A}", relativeFilename);
                        }
                    }
                    else
                    {
                        skipped++;
                        logger.LogTrace("Skip {A}", relativeFilename);
                    }
                }
            }
            sw.Stop();
            logger.LogInformation("{A} files copied, {B} files skipped in {C}s", copied, skipped, sw.ElapsedMilliseconds / 1000.0);
        }


        public override void DeleteFiles()
        {
            if (!(JobOptions is IFileCleanJobOptions jobOptions))
                throw new ArgumentException("this instance has no information about deleting files, it has type " + JobOptions.GetType().ToString());

            try
            {
                var driveInfo = new DriveInfo(Path.GetFullPath(JobOptions.DestinationPath));
                TimeSpan currentAge = jobOptions.MaxAge > TimeSpan.Zero ? jobOptions.MaxAge : TimeSpan.MaxValue;
                TimeSpan step = jobOptions.Interval > TimeSpan.Zero ? jobOptions.Interval : TimeSpan.FromHours(1);
                IEnumerable<string> paths ;
                if (JobOptions.Subfolders.Count == 0)
                    paths= new[] { (jobOptions.DestinationPath) };
                else
                    paths = JobOptions.Subfolders.Select(x => Path.Combine(JobOptions.DestinationPath, x));
                
                long minimumFreeSpace = Math.Max(2048, jobOptions.MinimumFreeSpaceMegabyte) * 1024L * 1024L;

                int fileCount = 0;
                
                bool lowSpace = false;
                while ((lowSpace = (driveInfo.AvailableFreeSpace < minimumFreeSpace && currentAge > JobOptions.Interval) || currentAge==jobOptions.MaxAge))
                {
                    List<FileInfo> filesToDelete = new List<FileInfo>();
                    
                    foreach (var pathMaxDay in paths)
                    {
                        try
                        {
                            if (!Directory.Exists(pathMaxDay))
                                Directory.CreateDirectory(pathMaxDay);
                            DirectoryInfo di = new DirectoryInfo(pathMaxDay);
                            FileInfo[] fi = di.GetFiles();
                            foreach (FileInfo f in fi)
                            {
                                if (f.LastWriteTime < (DateTime.Now - currentAge))
                                {
                                    filesToDelete.Add(f);
                                }
                            }
                        }
                        catch (Exception ex) { logger.LogError(ex, "exception getting file information of {A}", pathMaxDay); }
                    }
                    if (filesToDelete.Count > 0)
                        logger.LogWarning("free space on drive {A} {B} MB is below limit of {C} MB. Deleting {D} files older than {E}", driveInfo.RootDirectory, (driveInfo.AvailableFreeSpace / 1024L / 1024L), (minimumFreeSpace / 1024L / 1024L), filesToDelete.Count, currentAge);
                    //else
                    //    log.LogDebug("free space on drive {A} {B} MB is below limit of {C} MB. No files older than {D} found", driveInfo.RootDirectory, (driveInfo.AvailableFreeSpace / 1024L / 1024L), (minimumFreeSpace / 1024L / 1024L), timeDiff);
                    foreach (var file in filesToDelete)
                    {
                        logger.LogDebug("deleting file {A}", file.FullName);

                        try
                        {
                            file.Delete();
                            fileCount++;
                        }
                        catch (Exception ex) { logger.LogError(ex, "exception deleting file {A}", file.FullName); }
                    }
                    currentAge -= step;

                }
            }
            catch (Exception exc)
            {
                logger.LogError(exc, "TimerCleanup_Tick threw an exception");
            }
        }
        
        void EnsureAccess()
        {
            //#region Backup
            //if (!string.IsNullOrWhiteSpace(einst.BasicSettings["BackupPath"]))
            //{
            //    //Dateien sichern:
            //    bool useCredentials = ((string)einst.BasicSettings["BackupPath"]).StartsWith(@"\\");
            //    string backupTarget = einst.BasicSettings["BackupPath"];

            //    if (backupTarget.StartsWith(@"\\")) //Netzwerk
            //    {
            //        try
            //        {
            //            //Probe network access:
            //            bool accesible = false;
            //            bool writeable = false;
            //            try
            //            {
            //                DirectoryInfo di = new DirectoryInfo(backupTarget);
            //                while (di != null && !di.Exists)
            //                {
            //                    di = di.Parent;
            //                }
            //                if (di != null && di.Exists)
            //                    accesible = true;
            //                if (accesible)
            //                {
            //                    Directory.CreateDirectory(backupTarget);
            //                    File.WriteAllText(backupTarget + "\\hri_write_probing", "probing write access");
            //                    try
            //                    {
            //                        File.Delete(backupTarget + "\\hri_write_probing");
            //                    }
            //                    catch (Exception exc)
            //                    {
            //                        log.LogWarning(exc, "Exception when deleting 'hri_write_probing' file, assuming writable");
            //                    }
            //                    writeable = true;
            //                }
            //            }
            //            catch { }
            //            if (!accesible || !writeable)
            //            {
            //                DirectoryInfo di = new DirectoryInfo(backupTarget);
            //                string netpath = di.Root.FullName;

            //                if (accesible) //Change credentials..
            //                {
            //                    NetworkShare.CancelExistingConnection(netpath);
            //                }

            //                string username = ((string)Einstellungen.BasicSettings["NetworkCredentials"]).Split(':')[0];
            //                string password = ((string)Einstellungen.BasicSettings["NetworkCredentials"]).Split(':')[1];
            //                NetworkCredential networkCredential = new NetworkCredential(username, password);
            //                NetworkShare net = null;
            //                try { net = new NetworkShare(netpath, networkCredential, true); }
            //                catch (Win32Exception exception) { log.LogInformation("Backup: Fehler beim herstellen einer Netzwerkverbindung zu " + netpath + ":\r\n" + exception.ToString()); }
            //            }
            //        }
            //        catch (Exception e)
            //        {
            //            log.LogError("backuptimer_tick", e);
            //            log.LogInformation("Backup: Fehler beim  Zugriff auf Netzlaufwerk:\r\n" + e.ToString());
            //        }

            //    }
            //    //else //Filesystem
            //    {
            //        try
            //        {
            //            //Beim Netzlaufwerk versuchen es erneut zu verbinden
            //            string drive = backupTarget.Split(':')[0];
            //            //if (!Map(drive + ":"))
            //            //    log.LogInformation("Fehler beim mappen des Laufwerkes " + drive + ": - Bitte Einstellungen überprüfen"); 
            //            bool canWrite = true;
            //            if (!Directory.Exists(backupTarget))
            //            {
            //                try
            //                {
            //                    Directory.CreateDirectory(backupTarget);
            //                }
            //                catch { log.LogInformation($"Konnte nicht auf Backuppfad {backupTarget} schreiben"); canWrite = false; }
            //            }
            //            if (canWrite)
            //                foreach (string folder in new string[] { "HriFFTLog", "HriShockLog", "HriLog", "HriDebugLog", "HRI" })
            //                {
            //                    string _path = Path.Combine(ProductionDataPath, folder);
            //                    if (!Directory.Exists(_path))
            //                        Directory.CreateDirectory(_path);
            //                    string destPath = Path.Combine(backupTarget, folder);
            //                    if (Directory.Exists(backupTarget))
            //                        if (!Directory.Exists(destPath))
            //                            Directory.CreateDirectory(destPath);
            //                    DirectoryInfo _di = new DirectoryInfo(_path);
            //                    FileInfo[] _fi = _di.GetFiles();
            //                    //Dateien ins Backup kopieren
            //                    foreach (FileInfo f in _fi)
            //                    {
            //                        bool copy = false;
            //                        string remotefilename = Path.Combine(destPath, f.Name);
            //                        if (!File.Exists(remotefilename))
            //                        {
            //                            copy = true;
            //                        }
            //                        else
            //                        {
            //                            FileInfo remoteFile = new FileInfo(remotefilename);
            //                            if (remoteFile.Length != f.Length)
            //                                copy = true;
            //                        }
            //                        if (copy)
            //                        {
            //                            File.Copy(f.FullName, remotefilename, true);
            //                            Thread.Sleep(15);
            //                        }
            //                        else
            //                        {
            //                            Thread.Sleep(5);
            //                        }
            //                    }
            //                }


            //        }
            //        catch (Exception e)
            //        {
            //            log.LogError("backuptimer_tick", e);
            //            log.LogInformation("Fehler beim erstellen des Backups!\r\n" + e.ToString());
            //        }
            //    }
            //}
            //#endregion


        }





    }
}
