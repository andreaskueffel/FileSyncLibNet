using System;
using System.Collections.Generic;
using System.Text;

namespace FileSyncLibNet.SyncProviders
{
    public enum SyncProvider
    {
        FileIO,
        SMBLib,
        Robocopy,
        SCP,
        Abstract=100
    }
}
