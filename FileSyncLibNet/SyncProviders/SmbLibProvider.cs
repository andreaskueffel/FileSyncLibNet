using FileSyncLibNet.Commons;
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





        public SmbLibProvider(IFileJobOptions options) : base(options)
        {

        }
        public override void DeleteFiles()
        {
            throw new NotImplementedException();
        }
        public override void SyncSourceToDest()
        {
            if (!(JobOptions is IFileSyncJobOptions jobOptions))
                throw new ArgumentException("this instance has no information about syncing files, it has type " + JobOptions.GetType().ToString());

            //Determine if source or destination is network path
            bool sourceIsNetShare = jobOptions.SourcePath.StartsWith("\\\\");
            bool destIsNetShare = jobOptions.DestinationPath.StartsWith("\\\\");
            if (sourceIsNetShare && destIsNetShare)
                throw new NotImplementedException("both source and destination are network shares - this is not supported yet");

            if (!sourceIsNetShare && !destIsNetShare)
                throw new Exception("neither source or destination are network share paths - unable to handle with SmbLibProvider");

            if (!sourceIsNetShare && destIsNetShare)
            {
                Directory.CreateDirectory(jobOptions.SourcePath);

                string DestinationServer = JobOptions.DestinationPath.StartsWith("smb://", StringComparison.OrdinalIgnoreCase) ?
                    JobOptions.DestinationPath.Substring(1) :
                    (JobOptions.DestinationPath.StartsWith("\\\\") ? JobOptions.DestinationPath.Substring(2) : JobOptions.DestinationPath).Split('\\')[0];

                string DestinationShare = JobOptions.DestinationPath.Substring(JobOptions.DestinationPath.IndexOf(DestinationServer) + DestinationServer.Length).Trim('/', '\\').Split('/', '\\')[0];
                string DestinationPath = JobOptions.DestinationPath.Substring(JobOptions.DestinationPath.IndexOf(DestinationShare) + DestinationShare.Length).Replace('/', '\\');




                //Dateien ins Backup kopieren
                if (JobOptions.Credentials != null)
                {
                    ConnectToShare(DestinationServer, DestinationShare, JobOptions.Credentials.Domain, JobOptions.Credentials.UserName, JobOptions.Credentials.Password);
                }
                else
                {
                    throw new ArgumentNullException("Credentials are not set - cannot connect to share");
                }

                DirectoryInfo _di = new DirectoryInfo(jobOptions.SourcePath);

                foreach (var dir in JobOptions.Subfolders.Count > 0 ? _di.GetDirectories() : new[] { _di })
                {
                    if (JobOptions.Subfolders.Count > 0 && !JobOptions.Subfolders.Select(x => x.ToLower()).Contains(dir.Name.ToLower()))
                        continue;


                    var _fi = dir.EnumerateFiles(
                    searchPattern: JobOptions.SearchPattern,
                    searchOption: JobOptions.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                    if (jobOptions.SyncDeleted)
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
                        var relativeFilename = f.FullName.Substring(Path.GetFullPath(jobOptions.SourcePath).Length);
                        var remotefile = Path.Combine(DestinationPath, relativeFilename.TrimStart('\\', '/')).Replace('/', '\\');
                        var exists = FileExists(remotefile, out long size);
                        copy = !exists || size != f.Length;
                        if (copy)
                        {
                            logger.LogDebug("Copy {A}", relativeFilename);
                            try
                            {
                                WriteFile(f.FullName, remotefile);
                                if (jobOptions.DeleteSourceAfterBackup)
                                {
                                    File.Delete(f.FullName);
                                }
                            }
                            catch (Exception exc)
                            {
                                logger.LogError(exc, "Exception in WriteFile for {A}", relativeFilename);
                            }

                        }
                        else
                            logger.LogDebug("Skip {A}", relativeFilename);
                    }
                }
            }
            else //(sourceIsNetShare && !destIsNetShare)
            {
                string SourceServer = jobOptions.SourcePath.StartsWith("smb://", StringComparison.OrdinalIgnoreCase) ?
                   jobOptions.SourcePath.Substring(1) :
                   (jobOptions.SourcePath.StartsWith("\\\\") ? jobOptions.SourcePath.Substring(2) : jobOptions.SourcePath).Split('\\')[0];

                string SourceShare = jobOptions.SourcePath.Substring(jobOptions.SourcePath.IndexOf(SourceServer) + SourceServer.Length).Trim('/', '\\').Split('/', '\\')[0];
                string SourcePath = jobOptions.SourcePath.Substring(jobOptions.SourcePath.IndexOf(SourceShare) + SourceShare.Length).Replace('/', '\\');

                if (JobOptions.Credentials != null)
                {
                    ConnectToShare(SourceServer, SourceShare, JobOptions.Credentials.Domain, JobOptions.Credentials.UserName, JobOptions.Credentials.Password);
                }
                else
                {
                    throw new ArgumentNullException("Credentials are not set - cannot connect to share");
                }

                DirectoryInfo _di = new DirectoryInfo(jobOptions.DestinationPath);

                foreach (var dir in JobOptions.Subfolders.Count > 0 ? _di.GetDirectories() : new[] { _di })
                {
                    if (JobOptions.Subfolders.Count > 0 && !JobOptions.Subfolders.Select(x => x.ToLower()).Contains(dir.Name.ToLower()))
                        continue;


                    var _fi = dir.EnumerateFiles(
                    searchPattern: JobOptions.SearchPattern,
                    searchOption: JobOptions.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                    var remoteFiles = ListFiles(SourcePath, true);
                    if (jobOptions.SyncDeleted)
                    {
                        foreach (var file in remoteFiles)
                        {
                            var realFilePath = file.Substring(file.IndexOf(SourcePath) + SourcePath.Length).Trim('\\').Replace('/', '\\');
                            if (!_fi.Any(x => x.FullName.Replace('/', '\\').EndsWith(realFilePath)))
                                File.Delete(file);
                        }
                    }
                    foreach (var remoteFile in remoteFiles)
                    {
                        bool copy = false;
                        var realFilePath = remoteFile.Substring(remoteFile.IndexOf(SourcePath) + SourcePath.Length).Trim('\\').Replace('/', '\\');
                        
                        var localFile = Path.Combine(jobOptions.DestinationPath, realFilePath.TrimStart('\\', '/')).Replace('/', '\\');
                        var exists = File.Exists(localFile);
                        _ = FileExists(remoteFile, out long remoteSize);
                        var size = exists ? new FileInfo(localFile).Length : 0;
                        copy = !exists || size != remoteSize;
                        if (copy)
                        {
                            logger.LogDebug("Copy {A}", realFilePath);
                            try
                            {
                                ReadFile(remoteFile, localFile);
                                if (jobOptions.DeleteSourceAfterBackup)
                                {
                                    DeleteFile(remoteFile);
                                }
                            }
                            catch (Exception exc)
                            {
                                logger.LogError(exc, "Exception in ReadFile for {A}", realFilePath);
                            }

                        }
                        else
                            logger.LogDebug("Skip {A}", realFilePath);
                    }
                }


            }
        }






        public void ConnectToShare(string server, string shareName, string domain, string user, string password)
        {
            NTStatus status;
            client = new SMB2Client(); // SMB2Client can be used as well
            bool isConnected = client.Connect(server, SMBTransportType.DirectTCPTransport);
            if (isConnected)
            {
                logger.LogDebug("ConnectToShare with domain {A} and user {B} with {C} pass", domain, user, password.Select(x => '*'));
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

        public void ReadFile(string remoteFilePath, string localFilePath)
        {
            object fileHandle;
            FileStatus fileStatus;

            var status = fileStore.CreateFile(out fileHandle, out fileStatus, remoteFilePath, AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.Read, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (status == NTStatus.STATUS_SUCCESS)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));
                var stream = File.Open(localFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                byte[] data;
                long bytesRead = 0;
                while (true)
                {
                    status = fileStore.ReadFile(out data, fileHandle, bytesRead, (int)client.MaxReadSize);
                    if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
                    {
                        throw new Exception("Failed to read from file");
                    }

                    if (status == NTStatus.STATUS_END_OF_FILE || data.Length == 0)
                    {
                        break;
                    }
                    bytesRead += data.Length;
                    stream.Write(data, 0, data.Length);
                }
                stream.Close();
            }
            if (fileHandle != null)
                status = fileStore.CloseFile(fileHandle);
            var fileInfo = new FileInfo(localFilePath);
            GetFileAttributes(remoteFilePath, out DateTime lastWriteTime, out DateTime creationTime, out _, out DateTime lastAccessTime);
            fileInfo.LastWriteTime = lastWriteTime;
            fileInfo.CreationTime = creationTime;
            fileInfo.LastAccessTime = lastAccessTime;
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
        void SetFileAttributes(string filepathFromShare, DateTime lastWriteTime, DateTime createTime, DateTime modifiedTime, DateTime accessTime)
        {
            object directoryHandle;
            FileStatus fileStatus;
            var status = fileStore.CreateFile(out directoryHandle, out fileStatus, filepathFromShare.Trim('\\'), AccessMask.GENERIC_READ, FileAttributes.Normal, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE, null);
            if (status == NTStatus.STATUS_SUCCESS)
            {
                
                status = fileStore.GetFileInformation(out FileInformation result, directoryHandle, FileInformationClass.FileBasicInformation);
                (result as FileBasicInformation).LastWriteTime=lastWriteTime;
                (result as FileBasicInformation).CreationTime=createTime;
                (result as FileBasicInformation).ChangeTime=modifiedTime;
                (result as FileBasicInformation).LastAccessTime=accessTime;
                status = fileStore.SetFileInformation(directoryHandle, result);
                status = fileStore.CloseFile(directoryHandle);
            }
            throw new Exception("unable to set attributes - status " + status);

        }
        void GetFileAttributes(string filepathFromShare, out DateTime lastWriteTime, out DateTime createTime, out DateTime modifiedTime, out DateTime accessTime)
        {
            object directoryHandle;
            FileStatus fileStatus;
            var status = fileStore.CreateFile(out directoryHandle, out fileStatus, filepathFromShare.Trim('\\'), AccessMask.GENERIC_READ, FileAttributes.Normal, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE, null);
            if (status == NTStatus.STATUS_SUCCESS)
            {

                status = fileStore.GetFileInformation(out FileInformation result, directoryHandle, FileInformationClass.FileBasicInformation);
                lastWriteTime = (result as FileBasicInformation).LastWriteTime.Time.Value;
                modifiedTime = (result as FileBasicInformation).ChangeTime.Time.Value;
                createTime = (result as FileBasicInformation).CreationTime.Time.Value;
                accessTime = (result as FileBasicInformation).LastAccessTime.Time.Value;
                status = fileStore.CloseFile(directoryHandle);
            }
            else
            {
                throw new Exception("unable to set attributes - status " + status);
            }

        }




    }
}
