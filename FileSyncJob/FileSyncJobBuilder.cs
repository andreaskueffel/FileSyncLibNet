using System;
using System.Collections.Generic;
using System.Text;

namespace FileSyncLibNet.FileSyncJob
{
    internal class FileSyncJobBuilder : IFileSyncJobBuilder, ICanBuild
    {
        FileSyncJobOptionsBuilder optionsBuilder;
        internal FileSyncJobBuilder()
        {

        }

        public IFileSyncJob Build()
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
        IFileSyncJob Build();
    }
}
