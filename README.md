# FileSyncLibNet

A library to easily backup or sync 2 folders either once or in a given interval.

> **Warning:**
> This lib is in an early development stage. Use at own risk.

# Usage

## Minimal

``` C#
var easySyncJob = FileSyncJob.CreateJob(FileSyncJobOptionsBuilder.CreateBuilder()
    .WithSourcePath("/data/db")
    .WithDestinationPath("/mnt/usb")
    .Build()
);
easySyncJob.ExecuteNow();
```

## Configurable Options

``` C#
var dbSyncJob = FileSyncJob.CreateJob(FileSyncJobOptionsBuilder.CreateBuilder()
    .WithSourcePath("/data/db")
    .WithDestinationPath(@"\\BackupServer\backups\database")
    .WithSearchPattern("*.db")
    .SyncRecursive(true)
    .SyncDeleted(false)
    .WithFileSyncProvider(FileSyncProvider.SMBLib)
    .WithCredentials("user", "pass")
    .WithInterval(TimeSpan.FromHours(1))
    .WithLogger(log)
    .Build()
);
dbSyncJob.StartJob();
```

