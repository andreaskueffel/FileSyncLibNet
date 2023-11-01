using System;
using System.Runtime.Serialization;

namespace FileSyncLibNet.FileSyncJob
{
    [Serializable]
    internal class FileSyncJobRunningException : Exception
    {
        public FileSyncJobRunningException()
        {
        }

        public FileSyncJobRunningException(string message) : base(message)
        {
        }

        public FileSyncJobRunningException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FileSyncJobRunningException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}