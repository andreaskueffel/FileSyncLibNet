using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FileSyncLibNet.FileSyncJob
{
    public interface IFileSyncJob
    {
        void StartJob();
        void ExecuteNow();
        Task ExecuteNowASync();
        event EventHandler<FileSyncJobEventArgs> JobStarted;
        event EventHandler<FileSyncJobEventArgs> JobFinished;
        event EventHandler<FileSyncJobEventArgs> JobError;

    }
}
