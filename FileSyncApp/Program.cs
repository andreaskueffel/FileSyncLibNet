using FileSyncLibNet.Commons;
using FileSyncLibNet.FileCleanJob;
using FileSyncLibNet.FileSyncJob;
using FileSyncLibNet.Logger;
using FileSyncLibNet.SyncProviders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace FileSyncApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("FileSyncApp - synchronizing folders and clean them up");
            Dictionary<string, IFileJobOptions> jobOptions = new Dictionary<string, IFileJobOptions>();
            var jsonSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new IgnorePropertyResolver(new string[] { "Logger" }),
                TypeNameHandling = TypeNameHandling.Auto,
            };
            if (!File.Exists("config.json"))
            {
                var cleanJob = FileCleanJobOptionsBuilder.CreateBuilder()
                   .WithDestinationPath("temp")
                   .WithInterval(TimeSpan.FromMinutes(20))
                   .WithMaxAge(TimeSpan.FromDays(500))
                   .WithMinimumFreeSpaceMegabytes(1024 * 30)
                   .Build();
                jobOptions.Add("CleanJob", cleanJob);

                var syncJobOption = FileSyncJobOptionsBuilder.CreateBuilder()
                    .WithSourcePath("\\\\192.168.214.240\\share\\hri\\production")
                    .WithDestinationPath("temp")
                    .WithFileSyncProvider(SyncProvider.SMBLib)

                    .WithCredentials(new System.Net.NetworkCredential("USER", "Password", ""))
                    .WithInterval(TimeSpan.FromMinutes(15))
                    /*
                    .WithSearchPattern("*.*")
                    */
                    .SyncRecursive(true)
                    
                    .Build();
                jobOptions.Add("SyncJob", syncJobOption);
                
                var json = JsonConvert.SerializeObject(jobOptions, Formatting.Indented, jsonSettings);
                File.WriteAllText("config.json", json);
            }
            var readJobOptions = JsonConvert.DeserializeObject<Dictionary<string, IFileJobOptions>>(File.ReadAllText("config.json"), jsonSettings);
            List<IFileJob> Jobs = new List<IFileJob>();
            foreach(var jobOption in readJobOptions)
            {
                jobOption.Value.Logger = new StringLogger(new Action<string>((x) => { Console.WriteLine(x); }) );
               Jobs.Add( FileSyncJob.CreateJob(jobOption.Value));
            }
            foreach(var job in Jobs)
            {
                job.StartJob();
            }
            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();


        }
    }

}