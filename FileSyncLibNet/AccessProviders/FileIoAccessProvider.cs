using FileSyncLibNet.Commons;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileSyncLibNet.AccessProviders
{
    internal class FileIoAccessProvider : IAccessProvider
    {
        public string AccessPath { get; private set; }
        private readonly ILogger logger;
        public FileIoAccessProvider(ILogger logger)
        {
            this.logger = logger;
        }
        public void UpdateAccessPath(string accessPath)
        {
            AccessPath = accessPath;
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(Path.Combine(AccessPath, path));
        }

        public FileInfo2 GetFileInfo(string path)
        {
            var fi = new FileInfo(Path.Combine(AccessPath, path));

            return new FileInfo2(path, fi.Exists)
            {
                LastWriteTime = fi.Exists ? fi.LastWriteTime : DateTime.MinValue,
                Length = fi.Exists ? fi.Length : 0
            };
        }

        public List<FileInfo2> GetFiles(DateTime minimumLastWriteTime, string pattern, string path = null, bool recursive = false, List<string> subfolders = null)
        {
            DirectoryInfo _di = new DirectoryInfo(AccessPath);
            List<FileInfo2> ret_val = new List<FileInfo2>();
            foreach (var dir in (subfolders != null && subfolders.Count > 0) ? _di.GetDirectories() : new[] { _di })
            {
                try
                {
                    if (subfolders != null && subfolders.Count > 0 && !subfolders.Select(x => x.ToLower()).Contains(dir.Name.ToLower()))
                        continue;
                    var _fi = dir.EnumerateFiles(
                        searchPattern: pattern,
                        searchOption: recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);


                    ret_val.AddRange(_fi.Select(x => new FileInfo2(x.FullName.Substring(AccessPath.Length + 1), x.Exists) { Length = x.Length, LastWriteTime = x.LastWriteTime }).ToList());
                }
                catch (Exception exc) {
                    logger?.LogError(exc, "exception in GetFiles for path {A}, dir {B}", path, dir);
                }
            }
            return ret_val;
        }

        public Stream GetStream(FileInfo2 file)
        {
            var realFilename = Path.Combine(AccessPath, file.Name);
            return File.OpenRead(realFilename);
        }

        public void WriteFile(FileInfo2 file, Stream content)
        {
            var realFilename = Path.Combine(AccessPath, file.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(realFilename));
            using (var stream = File.Create(realFilename))
            {
                content.CopyTo(stream);
            }
            File.SetLastWriteTime(realFilename, file.LastWriteTime);

        }
        public void Delete(FileInfo2 fileInfo)
        {
            var realFilename = Path.Combine(AccessPath, fileInfo.Name);
            File.Delete(realFilename);
        }
    }
}
