namespace FileSyncAppWin
{
    public partial class MainForm : Form
    {
        Thread consoleThread;
        string[] Args;

        public MainForm(string[] args)
        {
            InitializeComponent();
            Args = args;
            this.FormClosing += (s, e) => {
                NewLogOutput($"FormClosing, Reason {e.CloseReason}");
                FileSyncApp.Program.keepRunning = false; 
                Program.autoRestart = false;
                consoleThread?.Join(10_000);
                NewLogOutput($"FormClosing, Reason {e.CloseReason}, consoleThread joined");
            };
            
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
            StartConsoleThread();
            //this.WindowState = FormWindowState.Minimized;
        }

        void StartConsoleThread()
        {
            consoleThread = new Thread(() =>
            {
                FileSyncApp.Program.Main(Args);
            });
            consoleThread.Start();

        }
        void Restart()
        {
            FileSyncApp.Program.keepRunning = false;
            consoleThread?.Join(10_000);
            FileSyncApp.Program.keepRunning = true;
            StartConsoleThread();
        }


        private void NewLogOutput(string e)
        {

            this.BeginInvoke(() => { textBox1.Text = string.Join(Environment.NewLine, (new string[] { $"{DateTime.Now.ToString("HH:mm:ss.fff")} {e}" }).Concat(textBox1.Text.Split(Environment.NewLine).Take(1000))); });

        }

        private void btn_Config_Click(object sender, EventArgs e)
        {
            FileSyncAppConfigEditor.JobsEditForm jobsEditForm = new FileSyncAppConfigEditor.JobsEditForm("config.json");
            jobsEditForm.ShowDialog();
        }

        private void btn_Restart_Click(object sender, EventArgs e)
        {
            Restart();
        }
    }
}