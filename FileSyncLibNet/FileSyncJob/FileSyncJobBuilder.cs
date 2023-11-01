using System;
using System.Collections.Generic;
using System.Text;
using FileSyncLibNet.Commons;

namespace FileSyncLibNet.FileSyncJob
{
    internal class FileSyncJobBuilder : IFileSyncJobBuilder, ICanBuild
    {
        FileSyncJobOptionsBuilder optionsBuilder;
        internal FileSyncJobBuilder()
        {

        }

        public IFileJob Build()
        {
            return FileSyncJob.CreateJob(optionsBuilder.Build());
        }
        public IFileSyncJobOptionsBuilderSetSource WithOptions()
        {
            optionsBuilder= new FileSyncJobOptionsBuilder();
            return (IFileSyncJobOptionsBuilderSetSource)optionsBuilder;
        }
    }
    public interface IFileSyncJobBuilder
    {
        IFileSyncJobOptionsBuilderSetSource WithOptions();
    }
    public interface ICanBuild
    {
        IFileJob Build();
    }
}
