using FileSyncLibNet.Logger;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace FileSyncLibNet.FileSyncJob
{
    public class FileSyncJobOptionsBuilder :
        IFileSyncJobOptionsBuilderSetSource,
        IFileSyncJobOptionsBuilderSetDestination,
        IFileSyncJobOptionsBuilderSetProvider,
        IFileSyncJobOptionsBuilderSetProperties,
        IFileSyncJobOptionsBuilderCanBuild
    {
        private readonly IFileSyncJobOptions jobOptions;
        internal FileSyncJobOptionsBuilder()
        {
            jobOptions = new FileSyncJobOptions();
        }

        public static IFileSyncJobOptionsBuilderSetSource CreateBuilder()
        {
            return new FileSyncJobOptionsBuilder();
        }

        public IFileSyncJobOptionsBuilderSetDestination WithSourcePath(string path)
        {
            jobOptions.SourcePath = path;
            return this;
        }

        public IFileSyncJobOptionsBuilderSetProperties WithDestinationPath(string path)
        {
            jobOptions.DestinationPath = path;
            return this;
        }
        public IFileSyncJobOptionsBuilderSetProperties WithFileSyncProvider(FileSyncProvider provider)
        {
            jobOptions.FileSyncProvider = provider;
            return this;
        }

        public IFileSyncJobOptionsBuilderSetProperties SyncRecursive(bool syncRecursive)
        {
            jobOptions.Recursive = syncRecursive;
            return this;
        }

        public IFileSyncJobOptionsBuilderSetProperties SyncDeleted(bool syncDeleted)
        {
            jobOptions.SyncDeleted = syncDeleted;
            return this;
        }

        public IFileSyncJobOptionsBuilderSetProperties WithCredentials(string username, string password)
        {
            jobOptions.Credentials = new NetworkCredential(username, password);
            return this;
        }

        public IFileSyncJobOptionsBuilderSetProperties WithCredentials(NetworkCredential networkCredential)
        {
            jobOptions.Credentials = networkCredential;
            return this;
        }

        public IFileSyncJobOptionsBuilderSetProperties WithSearchPattern(string searchPattern)
        {
            jobOptions.SearchPattern = searchPattern;
            return this;
        }
        public IFileSyncJobOptionsBuilderSetProperties WithSubfolder(string subfolder)
        {
            jobOptions.Subfolders.Add(subfolder);
            return this;
        }
        public IFileSyncJobOptionsBuilderSetProperties WithInterval(TimeSpan interval)
        {
            jobOptions.Interval = interval;
            return this;
        }
        public IFileSyncJobOptionsBuilderSetProperties WithLogger(ILogger logger)
        {
            jobOptions.Logger = logger;
            return this;
        }
        public IFileSyncJobOptionsBuilderSetProperties WithLogger(Action<string> loggerAction)
        {
            jobOptions.Logger = new StringLogger(loggerAction);
            return this;
        }
        public IFileSyncJobOptions Build()
        {
            if (null == jobOptions.Logger)
                jobOptions.Logger = new StringLogger((x) => { });
            return jobOptions;
        }
        public IFileSyncJob BuildJob()
        {
            return FileSyncJob.CreateJob(Build());
        }

    }

    public interface IFileSyncJobOptionsBuilderSetSource
    {
        IFileSyncJobOptionsBuilderSetDestination WithSourcePath(string path);
    }
    public interface IFileSyncJobOptionsBuilderSetDestination
    {
        IFileSyncJobOptionsBuilderSetProperties WithDestinationPath(string path);
    }
    public interface IFileSyncJobOptionsBuilderSetProvider
    {
        IFileSyncJobOptionsBuilderSetProperties WithFileSyncProvider(FileSyncProvider provider);
    }

    public interface IFileSyncJobOptionsBuilderSetProperties : IFileSyncJobOptionsBuilderSetProvider, IFileSyncJobOptionsBuilderCanBuild
    {
        IFileSyncJobOptionsBuilderSetProperties WithInterval(TimeSpan interval);
        IFileSyncJobOptionsBuilderSetProperties SyncRecursive(bool syncRecursive);
        IFileSyncJobOptionsBuilderSetProperties SyncDeleted(bool syncDeleted);
        IFileSyncJobOptionsBuilderSetProperties WithCredentials(string username, string password);
        IFileSyncJobOptionsBuilderSetProperties WithCredentials(NetworkCredential networkCredential);
        IFileSyncJobOptionsBuilderSetProperties WithSearchPattern(string searchPattern);
        IFileSyncJobOptionsBuilderSetProperties WithSubfolder(string subfolder);
        IFileSyncJobOptionsBuilderSetProperties WithLogger(ILogger logger);
        IFileSyncJobOptionsBuilderSetProperties WithLogger(Action<string> loggerAction);
    }

    public interface IFileSyncJobOptionsBuilderCanBuild
    {
        IFileSyncJobOptions Build();
        IFileSyncJob BuildJob();
    }

}
