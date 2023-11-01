using System;

namespace FileSyncLibNet.FileSyncJob
{
    public class FileSyncJobEventArgs : EventArgs
    {
        public FileSyncJobEventArgs(string jobName, FileSyncJobStatus status, Exception exc = null)
        {
            JobName = jobName;
            Status = status;
            Exception = exc;
        }
        public Exception Exception { get; }
        public FileSyncJobStatus Status { get; }
        public string JobName { get; }

    }
}
