using FileSyncLibNet.Commons;
using FileSyncLibNet.FileCleanJob;
using FileSyncLibNet.FileSyncJob;
using Newtonsoft.Json;
using RoboSharp.Interfaces;
using System.Text.Json;

namespace FileSyncAppConfigEditor
{
    public partial class JobsEditForm : Form
    {
        private string configFilePath = "config.json";
        private Dictionary<string, IFileJobOptions> jobs;
        JsonSerializerSettings jsonSettings;

        public JobsEditForm(string initialFile=null)
        {
            InitializeComponent();
            Init();
            if(!string.IsNullOrEmpty(initialFile) && File.Exists(initialFile))
            {
                configFilePath = initialFile;
                LoadJobsFromFile(initialFile);
            }
        }

        void Init()
        {
            jsonSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new IgnorePropertyResolver(new string[] { "Logger" }),
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            };
            openFileDialog1.FileName = configFilePath;
            saveFileDialog1.FileName = configFilePath;
            pg_JobEdit.SelectedGridItemChanged += (s, ev) =>
            {
                if (ev.NewSelection != null)
                {
                    //var prop = ev.NewSelection.PropertyDescriptor;
                    //if (prop != null)
                    //{
                    //    var propValue = prop.GetValue(jobs[selectedJob]);
                    //    if (propValue != null)
                    //    {
                    //        if (prop.PropertyType == typeof(System.Net.NetworkCredential))
                    //        {
                    //            var cred = propValue as System.Net.NetworkCredential;
                    //            if (cred != null)
                    //            {
                    //                cred.Password = "";
                    //            }
                    //        }
                    //    }
                    //}
                }
            };
        }

        private void ButtonLoadJobs_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            var result = openFileDialog1.ShowDialog();
            if (result != DialogResult.OK)
                return;
            configFilePath = openFileDialog1.FileName;
            if (File.Exists(configFilePath))
            {
                LoadJobsFromFile(configFilePath);
            }
            else
            {
                jobs = new Dictionary<string, IFileJobOptions>();
            }
        }

        private void LoadJobsFromFile(string configFilePath)
        {
            string json = File.ReadAllText(configFilePath);
            jobs = JsonConvert.DeserializeObject<Dictionary<string, IFileJobOptions>>(json, jsonSettings);
            lb_Jobs.Items.Clear();
            foreach (var job in jobs.Keys)
            {
                lb_Jobs.Items.Add(job);
            }
        }

        private void ListBoxJobs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lb_Jobs.SelectedItem != null)
            {
                string selectedJob = lb_Jobs.SelectedItem.ToString();
                pg_JobEdit.SelectedObject = jobs[selectedJob];
               
                tb_JobName.Text = selectedJob; // Set the text box to the selected job name
            }
        }

        private void ButtonAddJob_Click(object sender, EventArgs e)
        {
            string newJobName = "NewJob_" + DateTime.Now.Ticks;
            IFileJobOptions newJob = null;
            if (sender == btn_Add)
            {
                newJob = new FileSyncJobOptions()
                {
                    SourcePath = "C:\\Source",
                    SyncDeleted = false,
                    DeleteSourceAfterBackup = false,
                    RememberLastSync = true,
                    MaxAge = TimeSpan.FromDays(10),
                    Credentials = new System.Net.NetworkCredential { UserName = "", Password = "", Domain = "" },
                    DestinationPath = "D:\\Destination",
                    Interval = TimeSpan.FromMinutes(5),
                    SearchPattern = "*.*",
                    Subfolders = new List<string>(),
                    Recursive = true,
                    FileSyncProvider = FileSyncLibNet.SyncProviders.SyncProvider.Abstract
                };
            }
            if (sender == btn_AddCleanJob)
            {
                newJob = new FileCleanJobOptions()
                {
                    DestinationPath = "C:\\temp",
                    Credentials = new System.Net.NetworkCredential { UserName = "", Password = "", Domain = "" },
                    MaxAge = TimeSpan.FromDays(10),
                    Interval = TimeSpan.FromMinutes(5),
                    FileSyncProvider = FileSyncLibNet.SyncProviders.SyncProvider.Abstract
                };
            }
            
            jobs.Add(newJobName, newJob);
            lb_Jobs.Items.Add(newJobName);
        }

        private void ButtonRemoveJob_Click(object sender, EventArgs e)
        {
            if (lb_Jobs.SelectedItem != null)
            {
                string selectedJob = lb_Jobs.SelectedItem.ToString();
                jobs.Remove(selectedJob);
                lb_Jobs.Items.Remove(selectedJob);
                pg_JobEdit.SelectedObject = null;
                tb_JobName.Text = ""; // Clear the text box
            }
        }

        private void ButtonSaveConfig_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            var result = saveFileDialog1.ShowDialog();
            if (result != DialogResult.OK)
                return;
            configFilePath = saveFileDialog1.FileName;
            string json = JsonConvert.SerializeObject(jobs, jsonSettings);
            File.WriteAllText(configFilePath, json);
            MessageBox.Show("Configuration saved successfully.", "Save Config", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ButtonRenameJob_Click(object sender, EventArgs e)
        {
            if (lb_Jobs.SelectedItem != null && !string.IsNullOrWhiteSpace(tb_JobName.Text))
            {
                string oldJobName = lb_Jobs.SelectedItem.ToString();
                string newJobName = tb_JobName.Text;

                if (jobs.ContainsKey(newJobName))
                {
                    MessageBox.Show("A job with this name already exists.", "Rename Job", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var job = jobs[oldJobName];
                jobs.Remove(oldJobName);
                jobs.Add(newJobName, job);

                int selectedIndex = lb_Jobs.SelectedIndex;
                lb_Jobs.Items[selectedIndex] = newJobName;
                lb_Jobs.SelectedIndex = selectedIndex;
            }
        }
    }
}
