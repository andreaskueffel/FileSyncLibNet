using FileSyncLibNet.Commons;
using FileSyncLibNet.FileCleanJob;
using FileSyncLibNet.FileSyncJob;
using FileSyncLibNet.SyncProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private static Microsoft.Extensions.Logging.ILogger log;
        public static Dictionary<string, IFileJob> Jobs = new Dictionary<string, IFileJob>();
        public static void Main(string[] args)
        {
            ConfigureLogger();
            log = LoggerFactory.CreateLogger("FileSyncAppMain");
            if (null != args && args.Length > 0)
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
            log.LogInformation("FileSyncApp - synchronizing folders and clean them up");
            Dictionary<string, IFileJobOptions> jobOptions = new Dictionary<string, IFileJobOptions>();
            var jsonSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new IgnorePropertyResolver(new string[] { "Logger" }),
                TypeNameHandling = TypeNameHandling.Auto,
            };
            if (!File.Exists("config.json"))
            {
                log.LogInformation("Config file {A} not found, creating a new one", "config.json");
                var cleanJob = FileCleanJobOptionsBuilder.CreateBuilder()
                   .WithDestinationPath("temp")
                   .WithInterval(TimeSpan.FromMinutes(21))
                   .WithMaxAge(TimeSpan.FromDays(30))
                   .WithMinimumFreeSpaceMegabytes(1024 * 30)
                   .Build();
                jobOptions.Add("CleanJob", cleanJob);

                var syncFromEdgeToLocal = FileSyncJobOptionsBuilder.CreateBuilder()
                    .WithSourcePath("\\\\edgeip\\share\\service\\production")
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
                    .WithDestinationPath("\\\\SERVER\\Share\\Subfolder\\" + hostname)
                    .WithFileSyncProvider(SyncProvider.FileIO)
                    .WithInterval(TimeSpan.FromMinutes(15))
                    .DeleteAfterBackup(false) //sonst werden die Daten wieder neu von der Edge geholt
                    .SyncRecursive(true)
                    .Build();
                jobOptions.Add("SyncFromLocalToRemote", syncFromLocalToRemote);

                var json = JsonConvert.SerializeObject(jobOptions, Formatting.Indented, jsonSettings);
                File.WriteAllText("config.json", json);
            }
            log.LogInformation("reading config file {A}", "config.json");
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
            log.LogInformation("Press Ctrl+C to exit");
            while (keepRunning)
            {
                Thread.Sleep(500);
            }
        }


        public static void ConfigureLogger()
        {
            if (LoggingLevel != null)
                return;
            LoggingLevel = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Information);
#if DEBUG
            LoggingLevel = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Verbose);
#endif
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
                    rollOnFileSizeLimit: true,
                    outputTemplate: serilogFileTemplate),
                    blockWhenFull: true)
                .MinimumLevel.ControlledBy(LoggingLevel)
                .CreateLogger();

            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { builder.AddSerilog(Serilog.Log.Logger); });
        }



    }

}