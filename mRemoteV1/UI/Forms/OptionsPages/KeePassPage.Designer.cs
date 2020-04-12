

namespace mRemoteNG.UI.Forms.OptionsPages
{
    public sealed partial class KeePassPage : OptionsPage
    {
        //UserControl overrides dispose to clean up the component list.
        [System.Diagnostics.DebuggerNonUserCode()]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && components != null)
                {
                    components.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        //Required by the Windows Form Designer
        private System.ComponentModel.Container components = null;

        //NOTE: The following procedure is required by the Windows Form Designer
        //It can be modified using the Windows Form Designer.
        //Do not modify it using the code editor.
        [System.Diagnostics.DebuggerStepThrough()]
        private void InitializeComponent()
        {
            this.btnGenerateSecretFile = new mRemoteNG.UI.Controls.Base.NGButton();
            this.chkKeePassIntegration = new mRemoteNG.UI.Controls.Base.NGCheckBox();
            this.KeePassFieldId = new mRemoteNG.UI.Controls.Base.NGComboBox();
            this.ngLabel1 = new mRemoteNG.UI.Controls.Base.NGLabel();
            this.ngKeePassStatus = new mRemoteNG.UI.Controls.Base.NGLabel();
            this.SuspendLayout();
            // 
            // btnGenerateSecretFile
            // 
            this.btnGenerateSecretFile._mice = mRemoteNG.UI.Controls.Base.NGButton.MouseState.OUT;
            this.btnGenerateSecretFile.Location = new System.Drawing.Point(335, 125);
            this.btnGenerateSecretFile.Margin = new System.Windows.Forms.Padding(4);
            this.btnGenerateSecretFile.Name = "btnGenerateSecretFile";
            this.btnGenerateSecretFile.Size = new System.Drawing.Size(183, 38);
            this.btnGenerateSecretFile.TabIndex = 5;
            this.btnGenerateSecretFile.Text = "Connect to KeePass";
            this.btnGenerateSecretFile.UseVisualStyleBackColor = true;
            this.btnGenerateSecretFile.Click += new System.EventHandler(this.BtnGenerateSecretFile_Click);
            // 
            // chkKeePassIntegration
            // 
            this.chkKeePassIntegration._mice = mRemoteNG.UI.Controls.Base.NGCheckBox.MouseState.OUT;
            this.chkKeePassIntegration.AutoSize = true;
            this.chkKeePassIntegration.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkKeePassIntegration.Location = new System.Drawing.Point(22, 28);
            this.chkKeePassIntegration.Margin = new System.Windows.Forms.Padding(4);
            this.chkKeePassIntegration.Name = "chkKeePassIntegration";
            this.chkKeePassIntegration.Size = new System.Drawing.Size(242, 27);
            this.chkKeePassIntegration.TabIndex = 1;
            this.chkKeePassIntegration.Text = "Enable KeePass Integration";
            this.chkKeePassIntegration.UseVisualStyleBackColor = true;
            // 
            // KeePassFieldId
            // 
            this.KeePassFieldId._mice = mRemoteNG.UI.Controls.Base.NGComboBox.MouseState.HOVER;
            this.KeePassFieldId.FormattingEnabled = true;
            this.KeePassFieldId.Items.AddRange(new object[] {
            "UserField"});
            this.KeePassFieldId.Location = new System.Drawing.Point(335, 61);
            this.KeePassFieldId.Name = "KeePassFieldId";
            this.KeePassFieldId.Size = new System.Drawing.Size(217, 31);
            this.KeePassFieldId.TabIndex = 6;
            // 
            // ngLabel1
            // 
            this.ngLabel1.AutoSize = true;
            this.ngLabel1.Location = new System.Drawing.Point(18, 69);
            this.ngLabel1.Name = "ngLabel1";
            this.ngLabel1.Size = new System.Drawing.Size(296, 23);
            this.ngLabel1.TabIndex = 7;
            this.ngLabel1.Text = "Choose field to use as KeePassEntryId";
            // 
            // ngKeePassStatus
            // 
            this.ngKeePassStatus.AutoSize = true;
            this.ngKeePassStatus.Location = new System.Drawing.Point(18, 140);
            this.ngKeePassStatus.Name = "ngKeePassStatus";
            this.ngKeePassStatus.Size = new System.Drawing.Size(79, 23);
            this.ngKeePassStatus.TabIndex = 8;
            this.ngKeePassStatus.Text = "ngLabel2";
            // 
            // KeePassPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.ngKeePassStatus);
            this.Controls.Add(this.ngLabel1);
            this.Controls.Add(this.KeePassFieldId);
            this.Controls.Add(this.chkKeePassIntegration);
            this.Controls.Add(this.btnGenerateSecretFile);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "KeePassPage";
            this.Size = new System.Drawing.Size(915, 735);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        internal Controls.Base.NGButton btnGenerateSecretFile;
        internal Controls.Base.NGCheckBox chkKeePassIntegration;
        private Controls.Base.NGComboBox KeePassFieldId;
        private Controls.Base.NGLabel ngLabel1;
        private Controls.Base.NGLabel ngKeePassStatus;
    }
}
