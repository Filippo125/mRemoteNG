using System;
using System.IO;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.Config.Putty;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Credential.KeePass;
using mRemoteNG.Tools;

namespace mRemoteNG.UI.Forms.OptionsPages
{
    public sealed partial class KeePassPage
    {
        public KeePassPage()
        {
            InitializeComponent();
            ApplyTheme();
            PageIcon = Resources.Config_Icon;
            var display = new DisplayProperties();
            this.KeePassFieldId.Items.Add("UserField");
            this.KeePassFieldId.SelectedItem = "UserField";
            if (KeePassHttpClient.ExistsConfig())
            {
                this.ngKeePassStatus.Text = "KeePass already integrated";
                this.ngKeePassStatus.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                this.ngKeePassStatus.Text = "KeePass not already integrated";
                this.ngKeePassStatus.ForeColor = System.Drawing.Color.Red;
            }

        }

        #region Public Methods

        public override string PageName
        {
            //get => Language.strTabAdvanced;
            get => "KeePass";
            set { }
        }

        public override void ApplyLanguage()
        {
            base.ApplyLanguage();

            //chkKeePassIntegration.Text = Language.strUseKeePass;
            //btnGenerateSecretFile.Text = Language.strGenerateSecretFile;
        }

        public override void LoadSettings()
        {
            chkKeePassIntegration.Checked = Settings.Default.UseKeePass;
            KeePassFieldId.SelectedValue = Settings.Default.KeePassField;
        }

        public override void SaveSettings()
        {
            Settings.Default.UseKeePass = chkKeePassIntegration.Checked ? true : false;
            Settings.Default.KeePassField = (string)KeePassFieldId.SelectedItem;
        }

        #endregion

        private void BtnGenerateSecretFile_Click(object sender, EventArgs e)
        {
            var keepassclient = new KeePassHttpClient(true);
            if (keepassclient.Authenticate())
            {
                System.Windows.Forms.MessageBox.Show("KeePass registration done!");
            }else
            {
                System.Windows.Forms.MessageBox.Show("KeePass registration failed!");
            }
        }
    }
}