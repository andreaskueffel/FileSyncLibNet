using FileSyncLibNet.Commons;
using System;
using System.Collections.Generic;
using System.IO;

namespace FileSyncLibNet.AccessProviders
{
    internal interface IAccessProvider
    {
        void CreateDirectory(string path);
        void UpdateAccessPath(string path);
        List<FileInfo2> GetFiles(DateTime minimumLastWriteTime, string pattern, string path = null, bool recursive = false, List<string> subfolders = null, bool olderFiles=false);
        FileInfo2 GetFileInfo(string path);
        void Delete(FileInfo2 fileInfo);
        Stream GetStream(FileInfo2 file);
        void WriteFile(FileInfo2 file, Stream content);
    }
}
