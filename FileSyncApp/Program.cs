using FileSyncLibNet.Commons;
using FileSyncLibNet.FileCleanJob;
using FileSyncLibNet.FileSyncJob;
using FileSyncLibNet.Logger;
using FileSyncLibNet.SyncProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using File = System.IO.File;

namespace FileSyncApp
{
    public class Program
    {
        public static event EventHandler JobsReady;
        public static volatile bool keepRunning = true;
        public static LoggingLevelSwitch LoggingLevel { get; private set; }
        public static ILoggerFactory LoggerFactory { get; private set; }
        public static Dictionary<string, IFileJob> Jobs = new Dictionary<string, IFileJob>();
        public static void Main(string[] args)
        {





            LoggingLevel= new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Information);
#if DEBUG
            LoggingLevel= new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Verbose);
#endif
            ConfigureLogger();
            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { builder.AddSerilog(Serilog.Log.Logger); });
            if (null!=args && args.Length > 0)
            {
                if (args.Contains("debug"))
                {
                    LoggingLevel.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
                }
            }
            Console.CancelKeyPress += (s, e) => { keepRunning = false; e.Cancel = true; };
            RunProgram();

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
                    .WithDestinationPath("\\\\sbr-verzsrvdmz\\Schwingungsueberwachung$\\Serienspektren_Import\\" + hostname)
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
            //List<IFileJob> Jobs = new List<IFileJob>();

            foreach (var jobOption in readJobOptions)
            {

                jobOption.Value.Logger = LoggerFactory.CreateLogger(jobOption.Key); 
                Jobs.Add(jobOption.Key, FileSyncJob.CreateJob(jobOption.Value));
            }
            JobsReady?.Invoke(null, EventArgs.Empty);
            foreach (var job in Jobs)
            {
                job.Value.StartJob();
            }
            Console.WriteLine("Press Ctrl+C to exit");
            while (keepRunning)
            {
                Thread.Sleep(200);
            }
        }


        private static void ConfigureLogger()
        {
            string serilogFileTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext:l} {Message:lj}{NewLine}{Exception}";
            string serilogConsoleTemplate = "{Timestamp:HH:mm:ss.fff}[{Level:u3}]{SourceContext:l} {Message:lj}{NewLine}{Exception}";
            Serilog.Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Async(asyncWriteTo => asyncWriteTo.Console(outputTemplate: serilogConsoleTemplate), blockWhenFull: true)
                .WriteTo.Async(asyncWriteTo => asyncWriteTo.File(
                    ("FileSyncApp.log"),
                    retainedFileCountLimit: 10,
                    fileSizeLimitBytes: 1024 * 1024 * 100,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: serilogFileTemplate),
                    blockWhenFull: true)
                .MinimumLevel.ControlledBy(LoggingLevel)
                .CreateLogger();


        }



    }

}