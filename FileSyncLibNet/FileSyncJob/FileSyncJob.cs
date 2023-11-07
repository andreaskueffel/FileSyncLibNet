using FileSyncLibNet.Commons;
using FileSyncLibNet.FileCleanJob;
using FileSyncLibNet.SyncProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileSyncLibNet.FileSyncJob
{
    public class FileSyncJob : IFileJob
    {
        public string JobName { get { return $"Type: {options.GetType()} Destination: {options.DestinationPath} ({options.Interval})"; } }
        public event EventHandler<FileSyncJobEventArgs> JobStarted;
        public event EventHandler<FileSyncJobEventArgs> JobFinished;
        public event EventHandler<FileSyncJobEventArgs> JobError;
        private readonly IFileJobOptions options;
        private readonly Timer timer;
        private readonly ISyncProvider syncProvider;
        private volatile bool v_jobRunning = false;

        private FileSyncJob(IFileJobOptions fileSyncJobOptions)
        {
            options = fileSyncJobOptions;
            timer = new Timer(TimerElapsed);
            switch (options.FileSyncProvider)
            {
                default:
                case SyncProvider.FileIO:
                    syncProvider = new FileIOProvider(fileSyncJobOptions);
                    break;
                case SyncProvider.SMBLib:
                    syncProvider = new SmbLibProvider(fileSyncJobOptions);
                    break;
                case SyncProvider.Robocopy:
                    syncProvider = new RoboCopyProvider(fileSyncJobOptions);
                    break;
            }
        }

        public static IFileJob CreateJob(IFileJobOptions fileSyncJobOptions)
        {
            return new FileSyncJob(fileSyncJobOptions);
        }
        public static IFileSyncJobBuilder CreateJobBuilder()
        {
            return new FileSyncJobBuilder();
        }


        public void ExecuteNow()
        {
            RunJobInterlocked();
        }
        public Task ExecuteNowASync()
        {
            return Task.Run(() => { RunJobInterlocked(); });
        }

        public void StartJob()
        {
            if (options.Interval != TimeSpan.Zero)
                timer.Change(TimeSpan.Zero, options.Interval);
            else
                ExecuteNow();
        }
        public void StopJob()
        {
            timer.Change(TimeSpan.Zero, TimeSpan.Zero);
        }
        private void TimerElapsed(object state)
        {
            RunJobInterlocked();
        }

        private void RunJobInterlocked()
        {
            if (v_jobRunning)
            {
                JobError?.Invoke(this, new FileSyncJobEventArgs(JobName, FileSyncJobStatus.Error, new FileSyncJobRunningException("A job is still running")));
                return;
            }
            v_jobRunning = true;
            JobStarted?.Invoke(this, new FileSyncJobEventArgs(JobName, FileSyncJobStatus.Running));
            try
            {
                //True Job Code
                options.Logger.LogInformation("start job {0}", JobName);
                if (options is IFileSyncJobOptions)
                    syncProvider.SyncSourceToDest();
                else if (options is IFileCleanJobOptions)
                    syncProvider.DeleteFiles();
                else
                    throw new NotImplementedException($"job with options type {options.GetType()}");
                options.Logger.LogInformation("end job {0}", JobName);
            }
            catch (Exception exc)
            {
                JobError?.Invoke(this, new FileSyncJobEventArgs(JobName, FileSyncJobStatus.Error, exc));
            }
            finally
            {
                v_jobRunning = false;
            }

            JobFinished?.Invoke(this, new FileSyncJobEventArgs(JobName, FileSyncJobStatus.Idle));
        }
    }
}
