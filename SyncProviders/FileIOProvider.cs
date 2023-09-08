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
        public FileIOProvider(IFileSyncJobOptions options)
        {
            JobOptions = options;
        }

        public override void SyncSourceToDest()
        {
            var sw = Stopwatch.StartNew();
            Directory.CreateDirectory(JobOptions.SourcePath);
            Directory.CreateDirectory(JobOptions.DestinationPath);
            int copied = 0;
            int skipped = 0;
            DirectoryInfo _di = new DirectoryInfo(JobOptions.SourcePath);
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

                    var relativeFilename = f.FullName.Substring(Path.GetFullPath(JobOptions.SourcePath).Length).TrimStart('\\');
                    var remotefile = new FileInfo(Path.Combine(JobOptions.DestinationPath, relativeFilename));
                    bool copy = !remotefile.Exists || remotefile.Length != f.Length || remotefile.LastWriteTime != f.LastWriteTime;
                    if (copy)
                    {
                        try
                        {
                            logger.LogDebug("Copy {A}", relativeFilename);
                            File.Copy(f.FullName, remotefile.FullName, true);
                            copied++;
                            if (JobOptions.DeleteSourceAfterBackup)
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



        void CopyFileWithBuffer(FileInfo source, FileInfo destination)
        {
            using (var outstream = new FileStream(destination.FullName, FileMode.Create, FileAccess.Write, FileShare.None, 4096 * 20))
            using (var instream = new FileStream(source.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096 * 20))
            {
                instream.CopyTo(outstream);
            }
            //destination.CreationTime = source.CreationTime;
            //destination.LastAccessTime = source.LastAccessTime;
            destination.LastWriteTime = source.LastWriteTime;
            //destination.Attributes = source.Attributes;

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
