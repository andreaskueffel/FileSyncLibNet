using FileSyncLibNet.Commons;
using FileSyncLibNet.FileCleanJob;
using FileSyncLibNet.FileSyncJob;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileSyncLibNet.SyncProviders
{
    internal class ScpProvider : ProviderBase
    {


        public ScpProvider(IFileJobOptions options) : base(options)
        {

        }

        private void CreateDirectoryRecursive(SftpClient client, string path)
        {
            var dirs = path.Split('/');
            string currentPath =path.StartsWith("/")?"/":"";
            for (int i = 0; i < dirs.Length; i++)
            {
                currentPath = (currentPath.EndsWith("/")?currentPath:(currentPath+"/")) + dirs[i];
                if (!string.IsNullOrEmpty(currentPath) && currentPath != "/")
                    if(!client.Exists(currentPath))
                    client.CreateDirectory(currentPath);
            }
        }

        public override void SyncSourceToDest()
        {
            if (!(JobOptions is IFileSyncJobOptions jobOptions))
                throw new ArgumentException("this instance has no information about syncing files, it has type " + JobOptions.GetType().ToString());
            var sw = Stopwatch.StartNew();
            //Format

            var pattern = @"scp://(?:(?<user>[^@]+)@)?(?<host>[^:/]+)(?::(?<port>\d+))?(?<path>/.*)?";
            var match = Regex.Match(JobOptions.DestinationPath, pattern);
            string path;
            SftpClient ftpClient;
            if (!match.Success)
            {
                throw new UriFormatException($"Unable to match scp pattern with given URL {JobOptions.DestinationPath}, use format scp://host:port/path");
            }
            else
            {
                var user = match.Groups["user"].Value;
                var host = match.Groups["host"].Value;
                var port = int.Parse(match.Groups["port"].Success ? match.Groups["port"].Value : "22"); // Default SCP port
                path = match.Groups["path"].Value;


                ftpClient = new SftpClient(host, port, JobOptions.Credentials.UserName, JobOptions.Credentials.Password);
            }
            ftpClient.Connect();
            CreateDirectoryRecursive(ftpClient, path);
            
            var remoteFiles = ftpClient.ListDirectory(path);

            Directory.CreateDirectory(jobOptions.SourcePath);

            int copied = 0;
            int skipped = 0;
            DirectoryInfo _di = new DirectoryInfo(jobOptions.SourcePath);
            //Dateien ins Backup kopieren
            if (JobOptions.Credentials != null)
            {

            }


            foreach (var dir in JobOptions.Subfolders.Count > 0 ? _di.GetDirectories() : new[] { _di })
            {
                if (JobOptions.Subfolders.Count > 0 && !JobOptions.Subfolders.Select(x => x.ToLower()).Contains(dir.Name.ToLower()))
                    continue;
                var _fi = dir.EnumerateFiles(
                    searchPattern: JobOptions.SearchPattern,
                    searchOption: JobOptions.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                if (jobOptions.RememberLastSync)
                {
                    var old = _fi.Count();
                    _fi = _fi.Where(x => x.LastWriteTime > (LastRun - jobOptions.Interval)).ToList();
                    skipped += old - _fi.Count();
                    LastRun = DateTimeOffset.Now;
                }
                foreach (FileInfo f in _fi)
                {

                    var relativeFilename = f.FullName.Substring(Path.GetFullPath(jobOptions.SourcePath).Length).TrimStart('\\');
                    var remoteFilename = Path.Combine(path, relativeFilename).Replace('\\', '/');
                    ISftpFile remotefile = null;
                    if (ftpClient.Exists(remoteFilename))
                    {
                        remotefile = ftpClient.Get(remoteFilename);
                    }
                    bool exists = remotefile != null;
                    bool lengthMatch = exists && remotefile.Length == f.Length;
                    bool timeMatch = exists && Math.Abs((remotefile.LastWriteTimeUtc -f.LastWriteTimeUtc).TotalSeconds)<1;

                    bool copy = !exists || !lengthMatch || !timeMatch;
                    if (copy)
                    {
                        try
                        {
                            logger.LogDebug("Copy {A}", relativeFilename);
                            CreateDirectoryRecursive(ftpClient, Path.GetDirectoryName(remoteFilename).Replace('\\', '/'));
                            
                            using (var fileStream = System.IO.File.OpenRead(f.FullName))
                            {
                                ftpClient.UploadFile(fileStream, remoteFilename);
                            }
                            ftpClient.SetLastWriteTimeUtc(remoteFilename, f.LastAccessTimeUtc);
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

            throw new NotImplementedException("deleting files via scp currently is not supported");
        }



    }
}
