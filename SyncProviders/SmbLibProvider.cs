using FileSyncLibNet.FileSyncJob;
using Microsoft.Extensions.Logging;
using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileAttributes = SMBLibrary.FileAttributes;

namespace FileSyncLibNet.SyncProviders
{
    internal class SmbLibProvider : ProviderBase
    {
        SMB2Client client;
        ISMBFileStore fileStore;
        string Server
        {
            get
            {
                if (JobOptions.DestinationPath.StartsWith("smb://", StringComparison.OrdinalIgnoreCase)) //Linux syntax
                {
                    //Pattern smb://ServerName/ShareName/Folder1/Folder2
                    return JobOptions.DestinationPath.Substring(1);
                }
                else
                {
                    //Pattern \\ServerName\ShareName\Folder1\Folder2
                    return (JobOptions.DestinationPath.StartsWith("\\\\") ? JobOptions.DestinationPath.Substring(2) : JobOptions.DestinationPath).Split('\\')[0];
                }
            }
        }
        string Share
        {
            get
            {
                var remainingDestination = JobOptions.DestinationPath.Substring(JobOptions.DestinationPath.IndexOf(Server) + Server.Length);
                return remainingDestination.Trim('/', '\\').Split('/', '\\')[0];
            }
        }
        string DestinationPath
        {
            get
            {
                return JobOptions.DestinationPath.Substring(JobOptions.DestinationPath.IndexOf(Share) + Share.Length).Replace('/', '\\');
            }
        }

        public SmbLibProvider(IFileSyncJobOptions options)
        {
            JobOptions = options;
            Directory.CreateDirectory(JobOptions.SourcePath);
            //Directory.CreateDirectory(JobOptions.DestinationPath);
        }

        public override void SyncSourceToDest()
        {
            DirectoryInfo _di = new DirectoryInfo(JobOptions.SourcePath);
            var _fi = _di.EnumerateFiles(
                searchPattern: JobOptions.SearchPattern,
                searchOption: JobOptions.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            //Dateien ins Backup kopieren
            if (JobOptions.Credentials != null)
            {
                ConnectToShare(Server, Share, JobOptions.Credentials.Domain, JobOptions.Credentials.UserName, JobOptions.Credentials.Password);
            }
            if (JobOptions.SyncDeleted)
            {
                var remoteFiles = ListFiles(DestinationPath, true);
                foreach (var file in remoteFiles)
                {
                    var realFilePath = file.Substring(file.IndexOf(DestinationPath) + DestinationPath.Length).Trim('\\').Replace('/', '\\');
                    if (!_fi.Any(x => x.FullName.Replace('/', '\\').EndsWith(realFilePath)))
                        DeleteFile(file);
                }
            }
            foreach (FileInfo f in _fi)
            {
                bool copy = false;
                var relativeFilename = f.FullName.Substring(Path.GetFullPath(JobOptions.SourcePath).Length);
                var remotefile = Path.Combine(DestinationPath, relativeFilename.TrimStart('\\', '/')).Replace('/', '\\');
                var exists = FileExists(remotefile, out long size);
                copy = !exists || size != f.Length;
                if (copy)
                {
                    logger.LogDebug("Copy {A}", relativeFilename);
                    //File.Copy(f.FullName, remotefile.FullName, true);
                    //CopyFileWithBuffer(f, remotefile);
                    WriteFile(f.FullName, remotefile);

                }
                else
                    logger.LogDebug("Skip {A}", relativeFilename);
            }
        }






        public void ConnectToShare(string server, string shareName, string domain, string user, string password)
        {
            NTStatus status;
            client = new SMB2Client(); // SMB2Client can be used as well
            bool isConnected = client.Connect(server, SMBTransportType.DirectTCPTransport);
            if (isConnected)
            {
                logger.LogDebug("ConnectToShare with domain {A} and user {B} with {C} pass", domain, user, password.Select(x=>'*'));
                status = client.Login(domain, user, password);
                if (status == NTStatus.STATUS_SUCCESS)
                {
                    //List<string> shares = client.ListShares(out status);
                    //client.Logoff();
                }
                else
                {
                    logger.LogError("ConnectToShare is status {A}", status);
                }
                //client.Disconnect();
            }
            fileStore = client.TreeConnect(shareName, out status);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new Exception("Failed to connect to share");
            }



        }

        public void Dispose()
        {
            fileStore?.Disconnect();
        }

        public void WriteFile(string localFilePath, string remoteFilePath)
        {
            NTStatus status;
            FileStream localFileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
            object fileHandle;
            FileStatus fileStatus;
            //Create folders recursive

            var paths = remoteFilePath.Trim('\\').Split('\\');
            if (paths.Length > 1)
            {
                string createpath = "";
                for (int i = 0; i < paths.Length - 1; i++)
                {
                    createpath = Path.Combine(createpath, paths[i]);
                    status = fileStore.CreateFile(out fileHandle, out fileStatus, createpath, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_OPEN_IF, CreateOptions.FILE_DIRECTORY_FILE, null);
                    if (status == NTStatus.STATUS_SUCCESS)
                    {
                        status = fileStore.CloseFile(fileHandle);
                    }
                }
            }

            status = fileStore.CreateFile(out fileHandle, out fileStatus, remoteFilePath.Trim('\\'), AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_SUPERSEDE, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
            if (status == NTStatus.STATUS_SUCCESS)
            {
                int writeOffset = 0;
                while (localFileStream.Position < localFileStream.Length)
                {
                    byte[] buffer = new byte[(int)client.MaxWriteSize];
                    int bytesRead = localFileStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead < (int)client.MaxWriteSize)
                    {
                        Array.Resize<byte>(ref buffer, bytesRead);
                    }
                    int numberOfBytesWritten;
                    status = fileStore.WriteFile(out numberOfBytesWritten, fileHandle, writeOffset, buffer);
                    if (status != NTStatus.STATUS_SUCCESS)
                    {
                        throw new Exception("Failed to write to file");
                    }
                    writeOffset += bytesRead;
                }
                status = fileStore.CloseFile(fileHandle);
            }
            localFileStream.Dispose();

        }
        void DeleteFile(string filePath)
        {
            object fileHandle;
            FileStatus fileStatus;
            var status = fileStore.CreateFile(out fileHandle, out fileStatus, filePath.Trim('\\'), AccessMask.GENERIC_WRITE | AccessMask.DELETE | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (status == NTStatus.STATUS_SUCCESS)
            {
                FileDispositionInformation fileDispositionInformation = new FileDispositionInformation();
                fileDispositionInformation.DeletePending = true;
                status = fileStore.SetFileInformation(fileHandle, fileDispositionInformation);
                bool deleteSucceeded = (status == NTStatus.STATUS_SUCCESS);
                status = fileStore.CloseFile(fileHandle);
            }

        }
        List<string> ListFiles(string subPath, bool recurse)
        {
            List<string> retval = new List<string>();
            object directoryHandle;
            FileStatus fileStatus;
            var status = fileStore.CreateFile(out directoryHandle, out fileStatus, subPath.Trim('\\'), AccessMask.GENERIC_READ, FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
            if (status == NTStatus.STATUS_SUCCESS)
            {
                List<QueryDirectoryFileInformation> fileList;
                status = fileStore.QueryDirectory(out fileList, directoryHandle, "*", FileInformationClass.FileDirectoryInformation);
                if (status == NTStatus.STATUS_SUCCESS || status == NTStatus.STATUS_NO_MORE_FILES)
                {

                    foreach (FileDirectoryInformation file in fileList.Where(x => x is FileDirectoryInformation))
                    {
                        if (file.FileName == "." || file.FileName == "..")
                            continue;

                        if ((file.FileAttributes &= FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            if (recurse)
                            {
                                try
                                {
                                    retval.AddRange(ListFiles(Path.Combine(subPath.Trim('\\'), file.FileName), recurse));
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            retval.Add(Path.Combine(subPath.Trim('\\'), file.FileName));
                        }

                    }

                }
                status = fileStore.CloseFile(directoryHandle);
            }
            return retval;
        }

        bool FileExists(string filepathFromShare, out long size)
        {
            object directoryHandle;
            FileStatus fileStatus;
            var status = fileStore.CreateFile(out directoryHandle, out fileStatus, filepathFromShare.Trim('\\'), AccessMask.GENERIC_READ, FileAttributes.Normal, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE, null);
            if (status == NTStatus.STATUS_SUCCESS)
            {
                status = fileStore.GetFileInformation(out FileInformation result, directoryHandle, FileInformationClass.FileStandardInformation);
                size = (result as FileStandardInformation).EndOfFile;
                status = fileStore.CloseFile(directoryHandle);
                return true;
            }
            size = 0;
            return false;

        }




    }
}
