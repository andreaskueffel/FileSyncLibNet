using FileSyncLibNet.FileSyncJob;
using Microsoft.Extensions.Logging;
using RoboSharp;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FileSyncLibNet.SyncProviders
{
    internal class RoboCopyProvider : ProviderBase
    {

        public RoboCopyProvider(IFileSyncJobOptions options)
        {
            JobOptions = options;
        }
        public override void SyncSourceToDest()
        {
            //Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance)
            RoboCommand backup = new RoboCommand();
            // events
            backup.OnFileProcessed += (s, e) => { logger.LogTrace($"{e.ProcessedFile.Name}:{e.ProcessedFile.FileClassType}"); };
            backup.OnCommandCompleted += (s, e) => { logger.LogDebug("ROBOCOPY: Command completed"); };

            // copy options
            backup.CopyOptions.Source = Path.GetFullPath(JobOptions.SourcePath);
            backup.CopyOptions.Destination = JobOptions.DestinationPath;
            backup.CopyOptions.CopySubdirectories = JobOptions.Recursive;
            backup.CopyOptions.Purge = JobOptions.SyncDeleted;
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

    }
}
