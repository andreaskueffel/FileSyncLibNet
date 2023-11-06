using FileSyncLibNet.Commons;
using FileSyncLibNet.FileCleanJob;
using FileSyncLibNet.FileSyncJob;
using FileSyncLibNet.Logger;
using FileSyncLibNet.SyncProviders;
using IWshRuntimeLibrary;
using Newtonsoft.Json;
using System;
using File = System.IO.File;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net;

namespace FileSyncApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                if (args.Contains("install"))
                {
                    InstallProgram();
                }
            }
            else
            {
                RunProgram();
            }



        }

        static void RunProgram()
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
                   .WithInterval(TimeSpan.FromMinutes(21))
                   .WithMaxAge(TimeSpan.FromDays(30))
                   .WithMinimumFreeSpaceMegabytes(1024 * 30)
                   .Build();
                jobOptions.Add("CleanJob", cleanJob);

                var syncFromEdgeToLocal = FileSyncJobOptionsBuilder.CreateBuilder()
                    .WithSourcePath("\\\\192.168.214.240\\share\\hri\\production")
                    .WithDestinationPath("temp")
                    .WithFileSyncProvider(SyncProvider.SMBLib)
                    .WithSubfolder("left")
                    .WithSubfolder("right")
                    .WithCredentials(new System.Net.NetworkCredential("USER", "Password", ""))
                    .WithInterval(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(25))
                    .SyncRecursive(true)
                    .Build();
                jobOptions.Add("SyncFromEdgeToLocal", syncFromEdgeToLocal);
                
                var hostname = Dns.GetHostName();
                
                var syncFromLocalToRemote = FileSyncJobOptionsBuilder.CreateBuilder()
                    .WithSourcePath("temp")
                    .WithDestinationPath("\\\\sbr-verzsrvdmz\\Schwingungsueberwachung$\\Serienspektren_Import\\"+hostname)
                    .WithFileSyncProvider(SyncProvider.FileIO)
                    .WithInterval(TimeSpan.FromMinutes(15))
                    .DeleteAfterBackup(false) //sonst werden die Daten wieder neu von der Edge geholt
                    .SyncRecursive(true)
                    .Build();
                jobOptions.Add("SyncFromLocalToRemote", syncFromLocalToRemote);

                var json = JsonConvert.SerializeObject(jobOptions, Formatting.Indented, jsonSettings);
                File.WriteAllText("config.json", json);
            }
            var readJobOptions = JsonConvert.DeserializeObject<Dictionary<string, IFileJobOptions>>(File.ReadAllText("config.json"), jsonSettings);
            List<IFileJob> Jobs = new List<IFileJob>();
            foreach (var jobOption in readJobOptions)
            {
                jobOption.Value.Logger = new StringLogger(new Action<string>((x) => { Console.WriteLine(x); }));
                Jobs.Add(FileSyncJob.CreateJob(jobOption.Value));
            }
            foreach (var job in Jobs)
            {
                job.StartJob();
            }
            Console.WriteLine("Press Ctrl+C or type 'q' to exit");
            while (Console.ReadLine() != "q")
            {

            }
        }
        static void InstallProgram()
        {
            // Get the path to the user's autorun folder
            string autorunFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            // Create a shortcut file name and path
            string shortcutName = "FileSyncApp.lnk"; // Change this to your program name
            string shortcutPath = System.IO.Path.Combine(autorunFolderPath, shortcutName);

            // Create a shortcut using Windows Script Host
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

            // Set the target and start in folder for the shortcut
            shortcut.TargetPath = Assembly.GetEntryAssembly().Location;
            shortcut.WorkingDirectory = Path.GetDirectoryName(shortcut.TargetPath);

            // Set shortcut window style to start minimized
            shortcut.WindowStyle = 7; // 7 represents "Minimized"

            // Save the shortcut
            shortcut.Save();

        }
    }

}