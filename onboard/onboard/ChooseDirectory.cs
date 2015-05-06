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
                    lblProgress.Text = "Logging in...";
                    lblProgress.Visible = true;
                    webpop.Close();
                    api = await BuildShareFileClient(args.AuthCode);
                    btnUpload.Enabled = true;
                    lblProgress.Visible = false;
                }
                else
                {
                    var authFailedAction = MessageBox.Show(String.Format("{0}: {1}", args.Error.Error, args.Error.ErrorDescription),
                        "Login Failed", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                    if(authFailedAction == DialogResult.Retry)
                    {
                        webpop.WebpopForm_Load(null, new EventArgs());
                    }
                    else
                    {
                        this.Close();
                    }
                }
            };
            webpop.ShowDialog();            
        }

        async void ChooseDirectory_Load2(object sender, EventArgs e)
        {
            // fohb1651-6fbd-4e43-9f82-441ac0752a47
            api = Program.GetZachApiClient();
            await Login(api);
            btnUpload.Enabled = true;
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
            string baseUriFormat = "https://{0}.{1}/sf/v3";
            string baseUri = String.Format(baseUriFormat, authCode.Subdomain, authCode.ApiControlPlane);
            IShareFileClient api = new ShareFileClient(baseUri);
            var oauth = new OAuthService(api, Globals.OAuthClientID, Globals.OAuthClientSecret);
            var token = await oauth.ExchangeAuthorizationCodeAsync(authCode);            
            api.AddOAuthCredentials(token);
            api.BaseUri = new Uri(String.Format(baseUriFormat, token.Subdomain, token.ApiControlPlane));
            await Login(api);
            return api;
        }

        async Task Login(IShareFileClient api)
        {
            var homeFolder = await api.Sessions.Get().Project(session => 
                ((ShareFile.Api.Models.User)session.Principal).HomeFolder).ExecuteAsync();
            
            txtSfPath.Text = homeFolder.Id;
        }

        private void btnBrowseLocal_Click(object sender, EventArgs e)
        {
            var folderSelector = new FolderBrowserDialog();
            folderSelector.RootFolder = Environment.SpecialFolder.MyComputer;
            
            if(folderSelector.ShowDialog() == DialogResult.OK)
            {
                txtLocalPath.Text = folderSelector.SelectedPath;
            }
        }

        private async void btnUpload_Click(object sender, EventArgs e)
        {
            if (!ValidateSfRoot(txtSfPath.Text, txtLocalPath.Text)) return;

            btnUpload.Enabled = false;
            btnBrowseLocal.Enabled = false;
            txtLocalPath.Enabled = false;
            txtSfPath.Enabled = false;
            lblProgress.Text = "Working...";
            lblProgress.Visible = true;
            try
            {
                var onboard = new Engine.Onboard(api, new Engine.OnDiskFileSystem(txtLocalPath.Text));
                var start = DateTimeOffset.Now;
                var result = await onboard.BeginUpload(api.Items.GetAlias(txtSfPath.Text));
                await result.WaitForFileUploads();
                var elapsed = DateTimeOffset.Now - start;
                lblProgress.Text = String.Format("Completed in {0}", elapsed);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblProgress.Visible = false;                
            }
            finally
            {
                btnUpload.Enabled = true;
                btnBrowseLocal.Enabled = true;
                txtLocalPath.Enabled = true;
                txtSfPath.Enabled = true;
            }
        }

        private bool ValidateSfRoot(string sfRootId, string localPath)
        {
            if (sfRootId.Equals("allshared", StringComparison.OrdinalIgnoreCase)
                && new System.IO.DirectoryInfo(localPath).EnumerateFiles().Count() > 0)
            {
                var result = MessageBox.Show("Files cannot be placed at the root of 'Shared Folders'. Any files in your local root directory will not be uploaded. Proceed anyway?",
                    "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                return result == System.Windows.Forms.DialogResult.Yes;
            }
            else
            {
                return true;
            }
        }
        
    }
}
