namespace devoUpdate
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
      this.btnUpload = new System.Windows.Forms.Button();
      this.groupBox = new System.Windows.Forms.GroupBox();
      this.cmbPort = new System.Windows.Forms.ComboBox();
      this.txtStatusInfo = new System.Windows.Forms.TextBox();
      this.gbFlashFile = new System.Windows.Forms.GroupBox();
      this.txtFlashFile = new System.Windows.Forms.TextBox();
      this.btnFlashBrowse = new System.Windows.Forms.Button();
      this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
      this.statusBar1 = new System.Windows.Forms.StatusStrip();
      this.tssStatus = new System.Windows.Forms.ToolStripStatusLabel();
      this.label1 = new System.Windows.Forms.Label();
      this.pictureBox1 = new System.Windows.Forms.PictureBox();
      this.btnAbout = new System.Windows.Forms.Button();
      this.groupBox.SuspendLayout();
      this.gbFlashFile.SuspendLayout();
      this.statusBar1.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
      this.SuspendLayout();
      // 
      // btnUpload
      // 
      this.btnUpload.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.btnUpload.Location = new System.Drawing.Point(12, 135);
      this.btnUpload.Name = "btnUpload";
      this.btnUpload.Size = new System.Drawing.Size(176, 27);
      this.btnUpload.TabIndex = 36;
      this.btnUpload.Text = "Upload";
      this.btnUpload.UseVisualStyleBackColor = true;
      this.btnUpload.Click += new System.EventHandler(this.btnUpload_Click);
      // 
      // groupBox
      // 
      this.groupBox.Controls.Add(this.cmbPort);
      this.groupBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.groupBox.Location = new System.Drawing.Point(12, 12);
      this.groupBox.Name = "groupBox";
      this.groupBox.Size = new System.Drawing.Size(474, 50);
      this.groupBox.TabIndex = 2;
      this.groupBox.TabStop = false;
      this.groupBox.Text = "Select Usb COM-Port:";
      // 
      // cmbPort
      // 
      this.cmbPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cmbPort.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.cmbPort.FormattingEnabled = true;
      this.cmbPort.Location = new System.Drawing.Point(6, 19);
      this.cmbPort.Name = "cmbPort";
      this.cmbPort.Size = new System.Drawing.Size(418, 24);
      this.cmbPort.TabIndex = 3;
      this.cmbPort.SelectedIndexChanged += new System.EventHandler(this.cmbPort_SelectedIndexChanged);
      // 
      // txtStatusInfo
      // 
      this.txtStatusInfo.AcceptsReturn = true;
      this.txtStatusInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtStatusInfo.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.txtStatusInfo.Location = new System.Drawing.Point(12, 197);
      this.txtStatusInfo.Multiline = true;
      this.txtStatusInfo.Name = "txtStatusInfo";
      this.txtStatusInfo.ReadOnly = true;
      this.txtStatusInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.txtStatusInfo.Size = new System.Drawing.Size(618, 192);
      this.txtStatusInfo.TabIndex = 50;
      // 
      // gbFlashFile
      // 
      this.gbFlashFile.Controls.Add(this.txtFlashFile);
      this.gbFlashFile.Controls.Add(this.btnFlashBrowse);
      this.gbFlashFile.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.gbFlashFile.Location = new System.Drawing.Point(12, 77);
      this.gbFlashFile.Name = "gbFlashFile";
      this.gbFlashFile.Size = new System.Drawing.Size(474, 50);
      this.gbFlashFile.TabIndex = 4;
      this.gbFlashFile.TabStop = false;
      this.gbFlashFile.Text = "Select Firmware:";
      // 
      // txtFlashFile
      // 
      this.txtFlashFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.txtFlashFile.Location = new System.Drawing.Point(6, 18);
      this.txtFlashFile.Name = "txtFlashFile";
      this.txtFlashFile.ReadOnly = true;
      this.txtFlashFile.Size = new System.Drawing.Size(418, 22);
      this.txtFlashFile.TabIndex = 6;
      this.txtFlashFile.TextChanged += new System.EventHandler(this.FlashFile_Changed);
      // 
      // btnFlashBrowse
      // 
      this.btnFlashBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.btnFlashBrowse.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.btnFlashBrowse.Location = new System.Drawing.Point(430, 16);
      this.btnFlashBrowse.Name = "btnFlashBrowse";
      this.btnFlashBrowse.Size = new System.Drawing.Size(38, 24);
      this.btnFlashBrowse.TabIndex = 7;
      this.btnFlashBrowse.Text = "...";
      this.btnFlashBrowse.UseVisualStyleBackColor = true;
      this.btnFlashBrowse.Click += new System.EventHandler(this.btnFlashBrowse_Click);
      // 
      // statusBar1
      // 
      this.statusBar1.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.statusBar1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tssStatus});
      this.statusBar1.Location = new System.Drawing.Point(0, 401);
      this.statusBar1.Name = "statusBar1";
      this.statusBar1.Size = new System.Drawing.Size(642, 22);
      this.statusBar1.TabIndex = 37;
      // 
      // tssStatus
      // 
      this.tssStatus.Name = "tssStatus";
      this.tssStatus.Size = new System.Drawing.Size(42, 17);
      this.tssStatus.Text = "Ready";
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.label1.Location = new System.Drawing.Point(9, 181);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(73, 13);
      this.label1.TabIndex = 52;
      this.label1.Text = "Status info";
      // 
      // pictureBox1
      // 
      this.pictureBox1.BackColor = System.Drawing.Color.White;
      this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
      this.pictureBox1.Location = new System.Drawing.Point(492, 12);
      this.pictureBox1.Name = "pictureBox1";
      this.pictureBox1.Size = new System.Drawing.Size(138, 59);
      this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
      this.pictureBox1.TabIndex = 53;
      this.pictureBox1.TabStop = false;
      this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
      // 
      // btnAbout
      // 
      this.btnAbout.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.btnAbout.Location = new System.Drawing.Point(560, 77);
      this.btnAbout.Name = "btnAbout";
      this.btnAbout.Size = new System.Drawing.Size(70, 27);
      this.btnAbout.TabIndex = 54;
      this.btnAbout.Text = "About";
      this.btnAbout.UseVisualStyleBackColor = true;
      this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
      // 
      // Form1
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(642, 423);
      this.Controls.Add(this.btnAbout);
      this.Controls.Add(this.pictureBox1);
      this.Controls.Add(this.label1);
      this.Controls.Add(this.gbFlashFile);
      this.Controls.Add(this.statusBar1);
      this.Controls.Add(this.txtStatusInfo);
      this.Controls.Add(this.groupBox);
      this.Controls.Add(this.btnUpload);
      this.Name = "Form1";
      this.Text = "Form1";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
      this.Load += new System.EventHandler(this.Form1_Load);
      this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseDown);
      this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseMove);
      this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseUp);
      this.Resize += new System.EventHandler(this.Form1_Resize);
      this.groupBox.ResumeLayout(false);
      this.gbFlashFile.ResumeLayout(false);
      this.gbFlashFile.PerformLayout();
      this.statusBar1.ResumeLayout(false);
      this.statusBar1.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnUpload;
        private System.Windows.Forms.GroupBox groupBox;
        private System.Windows.Forms.TextBox txtStatusInfo;
        private System.Windows.Forms.Button btnFlashBrowse;
        private System.Windows.Forms.TextBox txtFlashFile;
        private System.Windows.Forms.GroupBox gbFlashFile;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.StatusStrip statusBar1;
        private System.Windows.Forms.ToolStripStatusLabel tssStatus;
        private System.Windows.Forms.ComboBox cmbPort;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnAbout;
    }
}
