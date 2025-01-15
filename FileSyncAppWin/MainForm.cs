namespace FileSyncAppWin
{
    public partial class MainForm : Form
    {
        Thread consoleThread;

        public MainForm(string[] args)
        {
            InitializeComponent();
            this.FormClosing += (s, e) => { FileSyncApp.Program.keepRunning = false; consoleThread?.Join(10_000); };
            consoleThread = new Thread(() =>
            {
                FileSyncApp.Program.Main(args);
            });
            FileSyncApp.Program.JobsReady += (s, e) =>
            {
                foreach (var job in FileSyncApp.Program.Jobs)
                {
                    job.Value.JobStarted += (j, text) =>
                    {
                        NewLogOutput($"{job.Key} STARTED - {text.Status} {text.Exception}");
                    };
                    job.Value.JobFinished += (j, text) =>
                    {
                        NewLogOutput($"{job.Key} FINISHED - {text.Status} {text.Exception}");
                    };
                    job.Value.JobError += (j, text) =>
                    {
                        NewLogOutput($"{job.Key} ERROR - {text.Status} {text.Exception}");
                    };

                }


            };
            consoleThread.Start();
            
            this.Resize += ((s, e) =>
            {
                this.SuspendLayout();
                this.BeginInvoke(() =>
                {
                    if (WindowState == FormWindowState.Minimized && ShowInTaskbar)
                    {
                        ShowInTaskbar = false;
                        notifyIcon1.ShowBalloonTip(10_000, "FileSyncAppWin", "I am here", ToolTipIcon.Info);
                        notifyIcon1.Visible = true;
                    }
                });


                this.ResumeLayout();
            });
            notifyIcon1.Click += (s, e) =>
            {
                this.BeginInvoke(() => { WindowState = FormWindowState.Normal; ShowInTaskbar = true; });
            };
            notifyIcon1.BalloonTipClicked += (s, e) => { this.BeginInvoke(() => { WindowState = FormWindowState.Normal; ShowInTaskbar = true; }); };
            //notifyIcon1.BalloonTipShown += (s, e) => { ShowInTaskbar = false; };

            //this.WindowState = FormWindowState.Minimized;
        }




        private void NewLogOutput(string e)
        {

            this.BeginInvoke(() => { textBox1.Text = string.Join(Environment.NewLine, (new string[] { $"{DateTime.Now.ToString("HH:mm:ss.fff")} {e}" }).Concat(textBox1.Text.Split(Environment.NewLine).Take(1000))); });

        }
    }
}