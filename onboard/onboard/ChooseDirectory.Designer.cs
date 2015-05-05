namespace ShareFile.Onboard.UI
{
    partial class ChooseDirectory
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChooseDirectory));
            this.label1 = new System.Windows.Forms.Label();
            this.txtLocalPath = new System.Windows.Forms.TextBox();
            this.btnBrowseLocal = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtSfPath = new System.Windows.Forms.TextBox();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.btnUpload = new System.Windows.Forms.Button();
            this.lblProgress = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(31, 67);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(109, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Choose a local folder:";
            // 
            // txtLocalPath
            // 
            this.txtLocalPath.Location = new System.Drawing.Point(146, 64);
            this.txtLocalPath.Name = "txtLocalPath";
            this.txtLocalPath.Size = new System.Drawing.Size(225, 20);
            this.txtLocalPath.TabIndex = 1;
            this.txtLocalPath.Text = "C:\\users\\zachariahj\\documents\\syncdata\\syncdata_Average";
            // 
            // btnBrowseLocal
            // 
            this.btnBrowseLocal.Location = new System.Drawing.Point(377, 64);
            this.btnBrowseLocal.Name = "btnBrowseLocal";
            this.btnBrowseLocal.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseLocal.TabIndex = 2;
            this.btnBrowseLocal.Text = "Browse";
            this.btnBrowseLocal.UseVisualStyleBackColor = true;
            this.btnBrowseLocal.Click += new System.EventHandler(this.btnBrowseLocal_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 124);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(131, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Choose a ShareFile folder:";
            // 
            // txtSfPath
            // 
            this.txtSfPath.Location = new System.Drawing.Point(146, 121);
            this.txtSfPath.Name = "txtSfPath";
            this.txtSfPath.Size = new System.Drawing.Size(225, 20);
            this.txtSfPath.TabIndex = 4;
            // 
            // progress
            // 
            this.progress.Location = new System.Drawing.Point(12, 175);
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(440, 23);
            this.progress.TabIndex = 5;
            this.progress.Visible = false;
            // 
            // btnUpload
            // 
            this.btnUpload.Enabled = false;
            this.btnUpload.Location = new System.Drawing.Point(190, 246);
            this.btnUpload.Name = "btnUpload";
            this.btnUpload.Size = new System.Drawing.Size(75, 23);
            this.btnUpload.TabIndex = 6;
            this.btnUpload.Text = "Upload";
            this.btnUpload.UseVisualStyleBackColor = true;
            this.btnUpload.Click += new System.EventHandler(this.btnUpload_Click);
            // 
            // lblProgress
            // 
            this.lblProgress.Location = new System.Drawing.Point(110, 221);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(240, 13);
            this.lblProgress.TabIndex = 7;
            this.lblProgress.Text = "label3";
            this.lblProgress.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.lblProgress.Visible = false;
            // 
            // ChooseDirectory
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 281);
            this.Controls.Add(this.lblProgress);
            this.Controls.Add(this.btnUpload);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.txtSfPath);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnBrowseLocal);
            this.Controls.Add(this.txtLocalPath);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ChooseDirectory";
            this.Text = "ChooseDirectory";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtLocalPath;
        private System.Windows.Forms.Button btnBrowseLocal;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtSfPath;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.Button btnUpload;
        private System.Windows.Forms.Label lblProgress;
    }
}