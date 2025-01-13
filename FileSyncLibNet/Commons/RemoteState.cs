using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FileSyncLibNet.Commons
{
    internal class RemoteState : IDisposable
    {
        public Dictionary<string, FileInfo2> FileInfos { get; set; }
        public string Filename { get; private set; }
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        public RemoteState(string name)
        {
            Filename = $"RemoteState_{name}.json";
            FileInfos = LoadFromFile(Filename);
        }
        public FileInfo2 GetFileInfo(string path)
        {
            if (FileInfos.ContainsKey(path))
                return FileInfos[path];

            return new FileInfo2(path, false)
            {
                LastWriteTime = DateTime.MinValue,
                Length = 0
            };
        }
        public void SetFileInfo(string path, FileInfo2 fileInfo)
        {
            if (FileInfos.ContainsKey(path))
            {
                FileInfos[path] = fileInfo;
            }
            else
                FileInfos.Add(path, fileInfo);

            SaveToFile(Filename, FileInfos);
        }
        public void RemoveFileInfo(string path)
        {
            if (FileInfos.ContainsKey(path))
            {
                FileInfos.Remove(path);
            }
            SaveToFile(Filename, FileInfos);
        }
        private static Dictionary<string, FileInfo2> LoadFromFile(string filename)
        {
            if (File.Exists(filename))
            {
                try
                {
                    var fileContent = File.ReadAllText(filename);
                    var fileInfos = JsonSerializer.Deserialize<Dictionary<string, FileInfo2>>(fileContent);
                    return fileInfos;
                }
                catch
                {
                    return new Dictionary<string, FileInfo2>();
                }
            }
            return new Dictionary<string, FileInfo2>();
        }

        private static void SaveToFile(string filename, Dictionary<string, FileInfo2> fileInfos)
        {
            try
            {
                var fileContent = JsonSerializer.Serialize(fileInfos, jsonOptions);
                File.WriteAllText(filename, fileContent);
            }
            catch
            {

            }
        }

        public void Dispose()
        {
            FileInfos.Clear();
        }
    }
}
