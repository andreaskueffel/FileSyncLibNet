namespace FileSyncAppConfigEditor
{
    partial class JobsEditForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pg_JobEdit = new PropertyGrid();
            lb_Jobs = new ListBox();
            btn_Add = new Button();
            btn_Remove = new Button();
            btn_Save = new Button();
            btn_Load = new Button();
            openFileDialog1 = new OpenFileDialog();
            tb_JobName = new TextBox();
            btn_Rename = new Button();
            btn_AddCleanJob = new Button();
            saveFileDialog1 = new SaveFileDialog();
            SuspendLayout();
            // 
            // pg_JobEdit
            // 
            pg_JobEdit.Location = new Point(270, 12);
            pg_JobEdit.Name = "pg_JobEdit";
            pg_JobEdit.Size = new Size(518, 426);
            pg_JobEdit.TabIndex = 0;
            // 
            // lb_Jobs
            // 
            lb_Jobs.FormattingEnabled = true;
            lb_Jobs.ItemHeight = 15;
            lb_Jobs.Location = new Point(12, 12);
            lb_Jobs.Name = "lb_Jobs";
            lb_Jobs.Size = new Size(252, 319);
            lb_Jobs.TabIndex = 1;
            lb_Jobs.SelectedIndexChanged += ListBoxJobs_SelectedIndexChanged;
            // 
            // btn_Add
            // 
            btn_Add.Location = new Point(12, 382);
            btn_Add.Name = "btn_Add";
            btn_Add.Size = new Size(75, 23);
            btn_Add.TabIndex = 2;
            btn_Add.Text = "Add Sync Job";
            btn_Add.UseVisualStyleBackColor = true;
            btn_Add.Click += ButtonAddJob_Click;
            // 
            // btn_Remove
            // 
            btn_Remove.Location = new Point(189, 382);
            btn_Remove.Name = "btn_Remove";
            btn_Remove.Size = new Size(75, 23);
            btn_Remove.TabIndex = 3;
            btn_Remove.Text = "Remove";
            btn_Remove.UseVisualStyleBackColor = true;
            btn_Remove.Click += ButtonRemoveJob_Click;
            // 
            // btn_Save
            // 
            btn_Save.Location = new Point(189, 415);
            btn_Save.Name = "btn_Save";
            btn_Save.Size = new Size(75, 23);
            btn_Save.TabIndex = 4;
            btn_Save.Text = "Save";
            btn_Save.UseVisualStyleBackColor = true;
            btn_Save.Click += ButtonSaveConfig_Click;
            // 
            // btn_Load
            // 
            btn_Load.Location = new Point(108, 415);
            btn_Load.Name = "btn_Load";
            btn_Load.Size = new Size(75, 23);
            btn_Load.TabIndex = 5;
            btn_Load.Text = "Load";
            btn_Load.UseVisualStyleBackColor = true;
            btn_Load.Click += ButtonLoadJobs_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // tb_JobName
            // 
            tb_JobName.Location = new Point(12, 338);
            tb_JobName.Name = "tb_JobName";
            tb_JobName.Size = new Size(171, 23);
            tb_JobName.TabIndex = 6;
            // 
            // btn_Rename
            // 
            btn_Rename.Location = new Point(189, 337);
            btn_Rename.Name = "btn_Rename";
            btn_Rename.Size = new Size(75, 23);
            btn_Rename.TabIndex = 7;
            btn_Rename.Text = "Rename";
            btn_Rename.UseVisualStyleBackColor = true;
            btn_Rename.Click += ButtonRenameJob_Click;
            // 
            // btn_AddCleanJob
            // 
            btn_AddCleanJob.Location = new Point(93, 382);
            btn_AddCleanJob.Name = "btn_AddCleanJob";
            btn_AddCleanJob.Size = new Size(75, 23);
            btn_AddCleanJob.TabIndex = 8;
            btn_AddCleanJob.Text = "Add Clean Job";
            btn_AddCleanJob.UseVisualStyleBackColor = true;
            btn_AddCleanJob.Click += ButtonAddJob_Click;
            // 
            // JobsEditForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btn_AddCleanJob);
            Controls.Add(btn_Rename);
            Controls.Add(tb_JobName);
            Controls.Add(btn_Load);
            Controls.Add(btn_Save);
            Controls.Add(btn_Remove);
            Controls.Add(btn_Add);
            Controls.Add(lb_Jobs);
            Controls.Add(pg_JobEdit);
            Name = "JobsEditForm";
            Text = "JobsEditForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PropertyGrid pg_JobEdit;
        private ListBox lb_Jobs;
        private Button btn_Add;
        private Button btn_Remove;
        private Button btn_Save;
        private Button btn_Load;
        private OpenFileDialog openFileDialog1;
        private TextBox tb_JobName;
        private Button btn_Rename;
        private Button btn_AddCleanJob;
        private SaveFileDialog saveFileDialog1;
    }
}
