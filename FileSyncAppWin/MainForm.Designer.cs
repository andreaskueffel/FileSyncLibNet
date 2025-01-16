namespace FileSyncAppWin
{
    partial class MainForm
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            textBox1 = new TextBox();
            notifyIcon1 = new NotifyIcon(components);
            panel1 = new Panel();
            btn_Config = new Button();
            btn_Restart = new Button();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Dock = DockStyle.Fill;
            textBox1.Location = new Point(0, 0);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ReadOnly = true;
            textBox1.ScrollBars = ScrollBars.Vertical;
            textBox1.Size = new Size(732, 299);
            textBox1.TabIndex = 0;
            // 
            // notifyIcon1
            // 
            notifyIcon1.Icon = (Icon)resources.GetObject("notifyIcon1.Icon");
            notifyIcon1.Text = "FileSyncAppWin";
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.Controls.Add(textBox1);
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(732, 299);
            panel1.TabIndex = 1;
            // 
            // btn_Config
            // 
            btn_Config.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_Config.Location = new Point(562, 326);
            btn_Config.Name = "btn_Config";
            btn_Config.Size = new Size(75, 23);
            btn_Config.TabIndex = 2;
            btn_Config.Text = "Config";
            btn_Config.UseVisualStyleBackColor = true;
            btn_Config.Click += btn_Config_Click;
            // 
            // btn_Restart
            // 
            btn_Restart.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_Restart.Location = new Point(647, 326);
            btn_Restart.Name = "btn_Restart";
            btn_Restart.Size = new Size(75, 23);
            btn_Restart.TabIndex = 3;
            btn_Restart.Text = "Restart";
            btn_Restart.UseVisualStyleBackColor = true;
            btn_Restart.Click += btn_Restart_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(734, 361);
            Controls.Add(btn_Restart);
            Controls.Add(btn_Config);
            Controls.Add(panel1);
            Name = "MainForm";
            Text = "FileSyncAppWin";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TextBox textBox1;
        private NotifyIcon notifyIcon1;
        private Panel panel1;
        private Button btn_Config;
        private Button btn_Restart;
    }
}