using FileSyncLibNet.Commons;
using FileSyncLibNet.Logger;
using FileSyncLibNet.SyncProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace FileSyncLibNet.FileCleanJob
{
    public class FileCleanJobOptionsBuilder :
        IFileCleanJobOptionsBuilderSetDestination,
        IFileCleanJobOptionsBuilderSetProvider,
        IFileCleanJobOptionsBuilderSetProperties,
        IFileCleanJobOptionsBuilderCanBuild
    {
        private readonly IFileCleanJobOptions jobOptions;
        internal FileCleanJobOptionsBuilder()
        {
            jobOptions = new FileCleanJobOptions();
        }

        public static IFileCleanJobOptionsBuilderSetDestination CreateBuilder()
        {
            return new FileCleanJobOptionsBuilder();
        }



        public IFileCleanJobOptionsBuilderSetProperties WithDestinationPath(string path)
        {
            jobOptions.DestinationPath = path;
            return this;
        }
        public IFileCleanJobOptionsBuilderSetProperties WithFileSyncProvider(SyncProvider provider)
        {
            jobOptions.FileSyncProvider = provider;
            return this;
        }

        public IFileCleanJobOptionsBuilderSetProperties SyncRecursive(bool syncRecursive)
        {
            jobOptions.Recursive = syncRecursive;
            return this;
        }
        public IFileCleanJobOptionsBuilderSetProperties WithMaxAge(TimeSpan maxAge)
        {
            jobOptions.MaxAge = maxAge;
            return this;
        }
        public IFileCleanJobOptionsBuilderSetProperties WithMinimumFreeSpaceMegabytes(long minimumFreeSpace)
        {
            jobOptions.MinimumFreeSpaceMegabyte =minimumFreeSpace;
            return this;
        }


        public IFileCleanJobOptionsBuilderSetProperties WithCredentials(string username, string password)
        {
            jobOptions.Credentials = new NetworkCredential(username, password);
            return this;
        }

        public IFileCleanJobOptionsBuilderSetProperties WithCredentials(NetworkCredential networkCredential)
        {
            jobOptions.Credentials = networkCredential;
            return this;
        }

        public IFileCleanJobOptionsBuilderSetProperties WithSearchPattern(string searchPattern)
        {
            jobOptions.SearchPattern = searchPattern;
            return this;
        }
        public IFileCleanJobOptionsBuilderSetProperties WithSubfolder(string subfolder)
        {
            jobOptions.Subfolders.Add(subfolder);
            return this;
        }

        public IFileCleanJobOptionsBuilderSetProperties WithInterval(TimeSpan interval)
        {
            jobOptions.Interval = interval;
            return this;
        }
        public IFileCleanJobOptionsBuilderSetProperties WithLogger(ILogger logger)
        {
            jobOptions.Logger = logger;
            return this;
        }
        public IFileCleanJobOptionsBuilderSetProperties WithLogger(Action<string> loggerAction)
        {
            jobOptions.Logger = new StringLogger(loggerAction);
            return this;
        }
        public IFileJobOptions Build()
        {
            if (null == jobOptions.Logger)
                jobOptions.Logger = new StringLogger((x) => { });
            return jobOptions;
        }
        public IFileJob BuildJob()
        {
            return FileSyncJob.FileSyncJob.CreateJob(Build());
        }

    }


    public interface IFileCleanJobOptionsBuilderSetDestination
    {
        IFileCleanJobOptionsBuilderSetProperties WithDestinationPath(string path);
    }
    public interface IFileCleanJobOptionsBuilderSetProvider
    {
        IFileCleanJobOptionsBuilderSetProperties WithFileSyncProvider(SyncProvider provider);
    }

    public interface IFileCleanJobOptionsBuilderSetProperties : IFileCleanJobOptionsBuilderSetProvider, IFileCleanJobOptionsBuilderCanBuild
    {
        IFileCleanJobOptionsBuilderSetProperties WithInterval(TimeSpan interval);
        IFileCleanJobOptionsBuilderSetProperties SyncRecursive(bool syncRecursive);
        IFileCleanJobOptionsBuilderSetProperties WithCredentials(string username, string password);
        IFileCleanJobOptionsBuilderSetProperties WithCredentials(NetworkCredential networkCredential);
        IFileCleanJobOptionsBuilderSetProperties WithSearchPattern(string searchPattern);
        IFileCleanJobOptionsBuilderSetProperties WithSubfolder(string subfolder);
        IFileCleanJobOptionsBuilderSetProperties WithMaxAge(TimeSpan maxAge);
        IFileCleanJobOptionsBuilderSetProperties WithMinimumFreeSpaceMegabytes(long minimumFreeSpace);
        IFileCleanJobOptionsBuilderSetProperties WithLogger(ILogger logger);
        IFileCleanJobOptionsBuilderSetProperties WithLogger(Action<string> loggerAction);
    }

    public interface IFileCleanJobOptionsBuilderCanBuild
    {
        IFileJobOptions Build();
        IFileJob BuildJob();
    }

}
