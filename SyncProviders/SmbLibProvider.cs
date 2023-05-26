using FileSyncLibNet.FileSyncJob;
using Microsoft.Extensions.Logging;
using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.IO;
using FileAttributes = SMBLibrary.FileAttributes;

namespace FileSyncLibNet.SyncProviders
{
    internal class SmbLibProvider : ProviderBase
    {
        SMB2Client client;
        ISMBFileStore fileStore;


        public SmbLibProvider(IFileSyncJobOptions options)
        {
            JobOptions = options;
            Directory.CreateDirectory(JobOptions.SourcePath);
            Directory.CreateDirectory(JobOptions.DestinationPath);
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
                ConnectToShare("frei", JobOptions.Credentials.Domain, JobOptions.Credentials.UserName, JobOptions.Credentials.Password);
            }

            foreach (FileInfo f in _fi)
            {
                bool copy = false;
                var relativeFilename = f.FullName.Substring(Path.GetFullPath(JobOptions.SourcePath).Length);
                var remotefile = new FileInfo(Path.Combine(JobOptions.DestinationPath, relativeFilename.TrimStart('\\')));
                copy = !remotefile.Exists || remotefile.Length != f.Length;
                if (copy)
                {
                    logger.LogDebug("Copy {A}", relativeFilename);
                    //File.Copy(f.FullName, remotefile.FullName, true);
                    //CopyFileWithBuffer(f, remotefile);
                    CopyFileWithSmbLib(f, remotefile);

                }
                else
                    logger.LogDebug("Skip {A}", relativeFilename);
            }
        }



        void CopyFileWithBuffer(FileInfo source, FileInfo destination)
        {
            using (var outstream = new FileStream(destination.FullName, FileMode.Create, FileAccess.Write, FileShare.None, 4096 * 20))
            using (var instream = new FileStream(source.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096 * 20))
            {
                instream.CopyTo(outstream);
            }
            //destination.CreationTime = source.CreationTime;
            //destination.LastAccessTime = source.LastAccessTime;
            destination.LastWriteTime = source.LastWriteTime;
            //destination.Attributes = source.Attributes;

        }
        void CopyFileWithSmbLib(FileInfo source, FileInfo destination)
        {
            WriteFile(source.FullName, Path.Combine("DA\\Küffel\\NetworkCopyTest", destination.Name));
        }

        public void ConnectToShare(string sharePath, string domain, string user, string password)
        {
            NTStatus status;
            client = new SMB2Client(); // SMB2Client can be used as well
            bool isConnected = client.Connect("PRA33", SMBTransportType.DirectTCPTransport);
            if (isConnected)
            {
                status = client.Login(domain, user, password);
                if (status == NTStatus.STATUS_SUCCESS)
                {
                    //List<string> shares = client.ListShares(out status);
                    //client.Logoff();
                }
                //client.Disconnect();
            }
            fileStore = client.TreeConnect(sharePath, out status);
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
            status = fileStore.CreateFile(out fileHandle, out fileStatus, remoteFilePath, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_SUPERSEDE, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
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




    }
}
