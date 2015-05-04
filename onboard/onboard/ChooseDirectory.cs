using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShareFile.Api.Client;
using ShareFile.Api.Client.Extensions;
using ShareFile.Api.Client.Security.Authentication.OAuth2;

namespace ShareFile.Onboard.UI
{
    public partial class ChooseDirectory : Form
    {
        private IShareFileClient api;

        public ChooseDirectory()
        {
            InitializeComponent();
            Load += ChooseDirectory_Load;
        }

        void ChooseDirectory_Load(object sender, EventArgs e)
        {
            ShareFile.Sync.Authentication.WebpopBrowser.SetUseCurrentIERegistryKey();
            var webpop = new WebpopForm();
            webpop.browser.Finished += async (browser, args) =>
            {
                if(args.Success)
                {
                    this.Text = String.Format("Onboarding: {0}.{1}", args.AuthCode.Subdomain, args.AuthCode.ApplicationControlPlane);
                    webpop.Close();
                    api = await BuildShareFileClient(args.AuthCode);
                    btnUpload.Enabled = true;
                }
            };
            webpop.ShowDialog();            
        }

        private void Invoke(Action f)
        {
            if (InvokeRequired)
                BeginInvoke(f);
            else
                f();
        }

        async Task<IShareFileClient> BuildShareFileClient(OAuthAuthorizationCode authCode)
        {
            string baseUri = String.Format("https://{0}.{1}/sf/v3", authCode.Subdomain, authCode.ApiControlPlane);
            IShareFileClient api = new ShareFileClient(baseUri);
            var oauth = new OAuthService(api, Globals.OAuthClientID, Globals.OAuthClientSecret);
            var token = await oauth.ExchangeAuthorizationCodeAsync(authCode);
            api.AddOAuthCredentials(token);
            return api;
        }

        private void btnBrowseLocal_Click(object sender, EventArgs e)
        {
            var folderSelector = new FolderBrowserDialog();
            folderSelector.RootFolder = Environment.SpecialFolder.MyDocuments;
            
            if(folderSelector.ShowDialog() == DialogResult.OK)
            {
                txtLocalPath.Text = folderSelector.SelectedPath;
            }
        }

        private async void btnUpload_Click(object sender, EventArgs e)
        {
            btnUpload.Enabled = false;
            lblProgress.Text = "Working...";
            lblProgress.Visible = true;            
            var onboard = new Engine.Onboard(api, new Engine.OnDiskFileSystem(txtLocalPath.Text));
            await onboard.Upload(api.Items.GetAlias(txtSfPath.Text));
            btnUpload.Enabled = true;
            lblProgress.Text = "Completed";
        }
        
    }
}
