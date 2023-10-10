using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FileSyncLibNet.FileSyncJob;

namespace FileSyncLibNet.Commons
{
    public interface IFileJob
    {
        void StartJob();
        void ExecuteNow();
        Task ExecuteNowASync();
        event EventHandler<FileSyncJobEventArgs> JobStarted;
        event EventHandler<FileSyncJobEventArgs> JobFinished;
        event EventHandler<FileSyncJobEventArgs> JobError;

    }
}
