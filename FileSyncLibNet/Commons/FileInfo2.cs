using System;

namespace FileSyncLibNet.Commons
{
    internal class FileInfo2 //: FileSystemInfo
    {
        public FileInfo2(string name, bool exists = true)
        {
            Name = name;
            Exists = exists;
        }
        public long Length { get; set; }
        public DateTime LastWriteTime { get; set; }
        public bool Exists { get; }
        public string Name { get; }

        public override string ToString()
        {
            return $"{Name}, {LastWriteTime}, Size {Length}";
        }

    }
}
