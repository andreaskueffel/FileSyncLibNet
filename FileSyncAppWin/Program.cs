using Microsoft.Extensions.Logging;
using System.IO.Pipes;

namespace FileSyncAppWin
{
    internal static class Program
    {

        private static Mutex mutex = new Mutex(true, "PraewemaFileSyncAppWin");
        private static volatile bool autoRestart = true;
        private static ILogger logger;
        private static Form mainForm;
        private const string PipeName = "PraewemaFileSyncAppWinPipe";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            FileSyncApp.Program.ConfigureLogger(args.FirstOrDefault());
            logger = FileSyncApp.Program.LoggerFactory.CreateLogger("FileSyncAppWin");
            if (args.Contains("noautorestart"))
            {
                logger?.LogInformation("startup argument noautorestart given, settings autoRestart to false");
                autoRestart = false;
            }
            if (args.Contains("restart"))
            {
                logger?.LogInformation("startup argument restart given, sending exit signal to running instance");
                SendExitSignalToFirstInstance();
            }
            logger?.LogInformation("try to get mutex");
            // Try to acquire the mutex
            if (!mutex.WaitOne(TimeSpan.FromSeconds(15)))
            {
                // Mutex was not acquired, meaning another instance is running

                Console.WriteLine("FileSyncAppWin another instance is running, exiting.");
                logger?.LogInformation("FileSyncAppWin another instance is running, exiting.");
                return; // Exit the second instance
            }

            try
            {
                var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut);
                // Start listening for incoming connections
                pipeServer.BeginWaitForConnection(PipeConnectedCallback, pipeServer);

                logger?.LogInformation("FileSyncAppWin Main program starting...");
                Application.ThreadException += Application_ThreadException;
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                ApplicationConfiguration.Initialize();
                mainForm = new MainForm();
                Application.Run(mainForm);
            }
            catch (Exception exception)
            {
                logger?.LogError(exception, "FileSyncAppWin exception in main logic");
            }
            finally
            {
                logger?.LogInformation("FileSyncAppWin releasing mutex");
                mutex.ReleaseMutex();
            }

        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                logger?.LogCritical((Exception)e.ExceptionObject, "CurrentDomain_UnhandledException isTerminating {A}", e.IsTerminating);
            }
            catch (Exception castException)
            {
                logger?.LogCritical(castException, "CurrentDomain_UnhandledException, Exception object {@A}", e.ExceptionObject);
            }
            //if (e.IsTerminating)
            {
                RestartApplication();
            }

        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            logger?.LogCritical(e.Exception, "Application_ThreadException");
            RestartApplication();
        }

        private static void PipeConnectedCallback(IAsyncResult result)
        {
            var pipeServer = (NamedPipeServerStream)result.AsyncState;

            try
            {
                // End waiting for the connection
                pipeServer.EndWaitForConnection(result);

                // Read the message from the pipe
                using (var reader = new StreamReader(pipeServer))
                {
                    string message = reader.ReadLine();
                    if (message == "Exit")
                    {
                        logger?.LogInformation("Received exit signal from second instance. Exiting gracefully.");
                        mainForm.Close();
                        //Environment.Exit(0); // Gracefully exit the application
                    }
                }

                // Close the pipe
                pipeServer.Close();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error handling named pipe connection.");
            }
        }

        private static void SendExitSignalToFirstInstance()
        {
            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    pipeClient.Connect(500); // Connect with a timeout

                    // Send an exit signal
                    using (var writer = new StreamWriter(pipeClient))
                    {
                        writer.WriteLine("Exit");
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error sending exit signal to first instance.");
            }
        }


        private static void RestartApplication()
        {
            if (autoRestart)
            {
                // Get the path of the current application
                string assemblyPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                // Restart the application using the path
                logger?.LogInformation("RestartApplication, starting new process {A}", assemblyPath);
                System.Diagnostics.Process.Start(assemblyPath);

                // Exit the current instance of the application
                Environment.Exit(0);
            }
            else
            {
                logger?.LogInformation("RestartApplication, skip starting new process -> autoRestart=false");
            }
        }
    }
}