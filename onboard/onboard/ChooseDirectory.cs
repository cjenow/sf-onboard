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
                    var authFailedAction = MessageBox.Show(this, String.Format("{0}: {1}", args.Error.Error, args.Error.ErrorDescription),
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
            webpop.ShowDialog(this);            
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
            
            if(folderSelector.ShowDialog(this) == DialogResult.OK)
            {
                txtLocalPath.Text = folderSelector.SelectedPath;
            }
        }

        private async void btnUpload_Click(object sender, EventArgs e)
        {
            if (!ValidateSfRoot(txtSfPath.Text, txtLocalPath.Text)) return;

            SetWorkingUI();
            try
            {
                var onboard = new Engine.Onboard(api, new Engine.OnDiskFileSystem(txtLocalPath.Text));
                var start = DateTimeOffset.Now;
                var result = await onboard.BeginUpload(api.Items.GetAlias(txtSfPath.Text));
                await result.FileUploadsFinished;
                var elapsed = DateTimeOffset.Now - start;
                lblProgress.Text = String.Format("Completed in {0}", elapsed);

                var retryAction = DisplayOnboardResult(result, elapsed);
                var retryResult = result;
                while(retryAction == DialogResult.Retry)
                {
                    SetWorkingUI();
                    // everything related to retry is awful, rip out and do over
                    var retryStart = DateTimeOffset.Now;
                    retryResult = await onboard.BeginRetryFailed(retryResult);
                    await retryResult.FileUploadsFinished;
                    var retryElapsed = DateTimeOffset.Now - retryStart;
                    lblProgress.Text = String.Format("Completed in {0}", retryElapsed);
                    UnsetWorkingUI();
                    retryAction = DisplayOnboardResult(retryResult, retryElapsed); 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblProgress.Visible = false;                
            }
            finally
            {
                UnsetWorkingUI();
            }
        }

        private void SetWorkingUI()
        {
            btnUpload.Enabled = false;
            btnBrowseLocal.Enabled = false;
            txtLocalPath.Enabled = false;
            txtSfPath.Enabled = false;
            lblProgress.Text = "Working...";
            lblProgress.Visible = true;
        }

        private void UnsetWorkingUI()
        {
            btnUpload.Enabled = true;
            btnBrowseLocal.Enabled = true;
            txtLocalPath.Enabled = true;
            txtSfPath.Enabled = true;
        }

        private bool ValidateSfRoot(string sfRootId, string localPath)
        {
            if (sfRootId.Equals("allshared", StringComparison.OrdinalIgnoreCase)
                && new System.IO.DirectoryInfo(localPath).EnumerateFiles().Count() > 0)
            {
                var result = MessageBox.Show(this, "Files cannot be placed at the root of 'Shared Folders'. Any files in your local root directory will not be uploaded. Proceed anyway?",
                    "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                return result == System.Windows.Forms.DialogResult.Yes;
            }
            else
            {
                return true;
            }
        }

        private DialogResult DisplayOnboardResult(Engine.OnboardResult result, TimeSpan elapsed)
        {
            var successfulFiles = result.AllFileResults.Where(file => file.UploadSucceeded).Select(file => file.File).ToArray();
            var failedFiles = result.AllFileResults.Where(file => !file.UploadSucceeded).Select(file => file.File).ToArray();
            string message = String.Format("{0} files uploaded successfully ({1})", successfulFiles.Length, successfulFiles.Sum(file => file.Size).ToFileSizeString());
            if(failedFiles.Length > 0)
            {
                message += String.Format("\n{0} files failed to upload ({1})", failedFiles.Length, failedFiles.Sum(file => file.Size).ToFileSizeString());
            }
            var failedFolders = result.AllFolderResults.Where(folder => !folder.CreateSucceeded).ToArray();
            if(failedFolders.Length > 0)
            {
                message += String.Format("\n{0} folders failed to upload", failedFolders.Length);
            }
            message += String.Format("\nElapsed time: {0}", elapsed);
            
            if(failedFolders.Length > 0 || failedFiles.Length > 0)
            {
                message += String.Format("\n\nRetry failed uploads?");
                return MessageBox.Show(this, message, "Upload Results", MessageBoxButtons.RetryCancel, MessageBoxIcon.Question);
            }
            else
            {
                return MessageBox.Show(this, message, "Upload Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        
    }

    static class DisplayExtensions
    {
        private static string[] FileSizeSuffixes = new[] { "bytes", "KB", "MB", "GB" };

        public static string ToFileSizeString(this long size)
        {
            int exp = (int)Math.Log(size, 1024);
            if (exp >= 0 && exp < FileSizeSuffixes.Length)
                return String.Format("{0} {1}", (size / Math.Pow(1024, exp)).ToString("F"), FileSizeSuffixes[exp]);
            else
                return String.Format("{0} {1}", size, FileSizeSuffixes[0]);
        }
    }
}
