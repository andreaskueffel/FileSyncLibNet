using FileSyncLibNet.FileSyncJob;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;

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
            var _fi = _di.EnumerateFiles(
                searchPattern: JobOptions.SearchPattern,
                searchOption: JobOptions.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            //Dateien ins Backup kopieren
            if (JobOptions.Credentials != null)
            {
                
            }

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
                    }
                    catch(Exception exc)
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
        




    }
}
