﻿using FileSyncLibNet.Commons;
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
            ConfigureLogger(args?.FirstOrDefault());
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
                    .WithSourcePath("scp://edgeip/service/production")
                    .WithDestinationPath("temp")
                    .WithFileSyncProvider(SyncProvider.Abstract)
                    .WithSubfolder("left")
                    .WithSubfolder("right")
                    .WithCredentials(new System.Net.NetworkCredential("USER", "Password", ""))
                    .WithInterval(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(25))
                    .SyncRecursive(true)
                    .Build();

                jobOptions.Add("SyncFromEdgeToLocal", syncFromEdgeToLocal);
                var clean = FileCleanJobOptionsBuilder.CreateBuilder()
                    .WithDestinationPath("\\\\sbrv\\share\\folder")
                    .WithFileSyncProvider(SyncProvider.FileIO)
                    .WithMaxAge(TimeSpan.FromDays(30))
                    .Build();

                jobOptions.Add("cleanJob", clean);

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
            Dictionary<string, IFileJobOptions> readJobOptions = new Dictionary<string, IFileJobOptions>();
            try
            {
                readJobOptions = JsonConvert.DeserializeObject<Dictionary<string, IFileJobOptions>>(File.ReadAllText("config.json"), jsonSettings);
            }
            catch (Exception exc)
            {
                log.LogCritical(exc, "exception reading config file {A}", "config.json");
                return;
            }

            foreach (var jobOption in readJobOptions)
            {
                jobOption.Value.Logger = LoggerFactory.CreateLogger(jobOption.Key);
                Jobs.Add(jobOption.Key, FileSyncJob.CreateJob(jobOption.Value));
            }
            JobsReady?.Invoke(null, EventArgs.Empty);
            Dictionary<IFileJob, bool> jobsDone = new Dictionary<IFileJob, bool>();
            foreach (var job in Jobs)
            {
                jobsDone.Add(job.Value, false);
                job.Value.JobFinished += (s, e) => { jobsDone[(IFileJob)s] = true; if (jobsDone.All(x => x.Value)) if (!File.Exists("fullsync.done")) File.Create("fullsync.done"); };
                job.Value.StartJob();

            }
            log.LogInformation("Press Ctrl+C to exit");
            while (keepRunning)
            {
                Thread.Sleep(500);
            }
            foreach(var job in Jobs)
            {
                job.Value.StopJob();
            }
            Jobs.Clear();
        }


        public static void ConfigureLogger(string logLevel)
        {
            Exception logLevelException = null;
            LoggingLevel = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Information);
            try
            {
                if (!string.IsNullOrEmpty(logLevel))
                {
                    LoggingLevel = new LoggingLevelSwitch((Serilog.Events.LogEventLevel)Enum.Parse(typeof(Serilog.Events.LogEventLevel), logLevel));
                }
            }
            catch (Exception exc)
            {
                logLevelException = exc;
            }

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
            if (null != logLevelException)
            {
                Serilog.Log.Error(logLevelException, "exception setting log level to {A}", logLevel);
            }
        }



    }

}