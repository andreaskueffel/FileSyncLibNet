using FileSyncLibNet.Commons;
using FileSyncLibNet.FileSyncJob;
using Microsoft.Extensions.Logging;
using RoboSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FileSyncLibNet.SyncProviders
{
    internal class RoboCopyProvider : ProviderBase
    {

        public RoboCopyProvider(IFileJobOptions options) : base(options)
        {

        }
        public override void SyncSourceToDest()
        {
            if (!(JobOptions is IFileSyncJobOptions jobOptions))
                throw new ArgumentException("this instance has no information about syncing files, it has type " + JobOptions.GetType().ToString());

            //Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance)
            RoboCommand backup = new RoboCommand();
            // events
            backup.OnFileProcessed += (s, e) => { logger.LogTrace($"{e.ProcessedFile.Name}:{e.ProcessedFile.FileClassType}"); };
            backup.OnCommandCompleted += (s, e) => { logger.LogDebug("ROBOCOPY: Command completed"); };

            // copy options
            backup.CopyOptions.Source = Path.GetFullPath(jobOptions.SourcePath);
            backup.CopyOptions.Destination = JobOptions.DestinationPath;
            backup.CopyOptions.CopySubdirectories = JobOptions.Recursive;
            backup.CopyOptions.Purge = jobOptions.SyncDeleted;
            backup.CopyOptions.FileFilter = new List<string>() { JobOptions.SearchPattern };
            //backup.CopyOptions.UseUnbufferedIo = true;
            backup.CopyOptions.MultiThreadedCopiesCount = System.Environment.ProcessorCount;
            
            backup.RetryOptions.RetryCount = 1;
            backup.RetryOptions.RetryWaitTime = 2;
            logger.LogDebug("Robocopy command: " + backup.CommandOptions);
            Task backupTask;
            if (false && null != JobOptions.Credentials)
            {
                //Different scope here! 
                backupTask = backup.Start(JobOptions.Credentials.Domain, JobOptions.Credentials.UserName, JobOptions.Credentials.Password);
            }
            else
            {
                backupTask = backup.Start();
            }
            Task.WaitAll(backupTask);
        }

        public override void DeleteFiles()
        {
            throw new NotImplementedException();
        }

    }
}
