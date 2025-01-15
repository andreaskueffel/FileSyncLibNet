using FileSyncLibNet.Commons;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace FileSyncLibNet.AccessProviders
{
    internal class ScpAccessProvider : IAccessProvider
    {
        public string AccessPathUri { get; private set; }
        NetworkCredential Credentials { get; }
        SftpClient ftpClient;
        private readonly RemoteState remoteState;
        public string AccessPath { get; private set; }

        private readonly ILogger logger;
        public ScpAccessProvider(NetworkCredential credentials, ILogger logger, string stateFilename)
        {
            if (!string.IsNullOrEmpty(stateFilename))
                remoteState = new RemoteState(stateFilename);
            this.logger = logger;
            Credentials = credentials;
        }
        public void UpdateAccessPath(string accessPath)
        {
            AccessPathUri = accessPath;
            var pattern = @"scp://(?:(?<user>[^@]+)@)?(?<host>[^:/]+)(?::(?<port>\d+))?(?<path>/.*)?";
            var match = Regex.Match(AccessPathUri, pattern);


            if (!match.Success)
            {
                throw new UriFormatException($"Unable to match scp pattern with given URL {AccessPathUri}, use format scp://host:port/path");
            }
            else
            {
                AccessPath = match.Groups["path"].Value;
                if (AccessPath.StartsWith("/~"))
                    AccessPath = AccessPath.Substring(1);
            }
        }


        void CreateClient()
        {
            var pattern = @"scp://(?:(?<user>[^@]+)@)?(?<host>[^:/]+)(?::(?<port>\d+))?(?<path>/.*)?";
            var match = Regex.Match(AccessPathUri, pattern);


            if (!match.Success)
            {
                throw new UriFormatException($"Unable to match scp pattern with given URL {AccessPathUri}, use format scp://host:port/path");
            }
            else
            {
                var user = match.Groups["user"].Value;
                var host = match.Groups["host"].Value;
                var port = int.Parse(match.Groups["port"].Success ? match.Groups["port"].Value : "22"); // Default SCP port
                AccessPath = match.Groups["path"].Value;
                if (AccessPath.StartsWith("/~"))
                    AccessPath = AccessPath.Substring(1);

                ftpClient = new SftpClient(host, port, Credentials.UserName, Credentials.Password);
            }
        }

        void EnsureConnected()
        {
            if (ftpClient == null)
            {
                CreateClient();
            }
            if (!ftpClient.IsConnected)
            {
                ftpClient.Connect();
            }
        }
        private void CreateDirectoryRecursive(SftpClient client, string path)
        {
            var dirs = path.Split('/');
            string currentPath = path.StartsWith("/") ? "/" : /*path.StartsWith("~") ? "~/" :*/ "";
            for (int i = 0; i < dirs.Length; i++)
            {
                if (dirs[i] == "~")
                    continue;
                currentPath = (currentPath.EndsWith("/") ? currentPath : currentPath == "" ? dirs[i] : ((currentPath + "/")) + dirs[i]);
                if (!string.IsNullOrEmpty(currentPath) && currentPath != "/" && currentPath != "/~")
                    if (!client.Exists(currentPath))
                        client.CreateDirectory(currentPath);
            }
        }


        public void CreateDirectory(string path)
        {
            EnsureConnected();
            var realDir = System.IO.Path.Combine(AccessPath, path).Replace("\\", "/");
            CreateDirectoryRecursive(ftpClient, realDir);
        }

        public FileInfo2 GetFileInfo(string path)
        {
            var realFilename = System.IO.Path.Combine(AccessPath, path).Replace("\\", "/");
            if (remoteState != null)
                return remoteState.GetFileInfo(realFilename);

            EnsureConnected();
            if (ftpClient.Exists(realFilename))
            {
                var fi = ftpClient.ListDirectory(realFilename);
                return new FileInfo2(path, true)
                {
                    LastWriteTime = fi.First().LastWriteTime,
                    Length = fi.First().Length
                };
            }
            else
            {
                return new FileInfo2(path, false)
                {
                    LastWriteTime = DateTime.MinValue,
                    Length = 0
                };
            }
        }

        public bool MatchesPattern(string fileName, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return true;
            string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
        }

        public List<FileInfo2> GetFiles(DateTime minimumLastWriteTime, string pattern, string path = null, bool recursive = false, List<string> subfolders = null, bool olderFiles = false)
        {
            //add try catch for non existent folders
            EnsureConnected();
            string basePath = string.IsNullOrEmpty(path) ? AccessPath : path;
            List<FileInfo2> ret_val = new List<FileInfo2>();
            if (subfolders != null && subfolders.Count > 0)
            {
                foreach (string subfolder in subfolders)
                {
                    var subPath = System.IO.Path.Combine(basePath, subfolder).Replace("\\", "/");
                    ret_val.AddRange(GetFiles(minimumLastWriteTime, pattern, path: subPath, recursive: recursive));
                }
            }
            else
            {
                try
                {
                    var files = ftpClient.ListDirectory(basePath);
                    var sepChar = "/";
                    if (recursive)
                    {
                        foreach (var folder in files.Where(x => x.IsDirectory && !(x.Name == ".") && !(x.Name == "..")))
                        {
                            var subPath = System.IO.Path.Combine(basePath, folder.Name).Replace("\\", "/");
                            ret_val.AddRange(GetFiles(minimumLastWriteTime, pattern, subPath, recursive));
                        }
                    }
                    ret_val.AddRange(files.Where(x => MatchesPattern(x.Name, pattern)).Where(x => (olderFiles ? x.LastWriteTime <= minimumLastWriteTime : x.LastWriteTime >= minimumLastWriteTime) && !x.IsDirectory).Select(x =>
                    new FileInfo2($"{(AccessPath.Length + 1 < basePath.Length ? (basePath.Substring(AccessPath.Length + 1)) + sepChar : string.Empty)}{x.Name}", exists: true) { LastWriteTime = x.LastWriteTime, Length = x.Length }).ToList());
                }
                catch (Exception exc)
                {
                    logger?.LogError(exc, "exception in GetFiles for path {A}, basePath {B}", path, basePath);
                }
            }
            return ret_val;
        }

        public Stream GetStream(FileInfo2 file)
        {
            EnsureConnected();
            var subPath = System.IO.Path.Combine(AccessPath, file.Name).Replace("\\", "/");
            return ftpClient.OpenRead(subPath);
        }

        public void WriteFile(FileInfo2 file, Stream content)
        {
            EnsureConnected();
            var filePath = System.IO.Path.Combine(AccessPath, file.Name).Replace("\\", "/");
            CreateDirectoryRecursive(ftpClient, filePath.Substring(0, filePath.LastIndexOf("/")));
            if (filePath.StartsWith("~/"))
                filePath = filePath.Substring(2);

            using (var stream = ftpClient.Open(filePath, FileMode.Create))
            {

                content.CopyTo(stream);
            }
            remoteState?.SetFileInfo(filePath, file);
        }

        public void Delete(FileInfo2 fileInfo)
        {
            EnsureConnected();
            var filePath = System.IO.Path.Combine(AccessPath, fileInfo.Name).Replace("\\", "/");
            ftpClient.Delete(filePath);
            remoteState?.RemoveFileInfo(filePath);
        }
    }
}
