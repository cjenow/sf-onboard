using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShareFile.Sync.Authentication;
using ShareFile.Api.Client.Security.Authentication.OAuth2;

namespace ShareFile.Onboard.UI
{
    public partial class WebpopForm : Form
    {
        public OAuthAuthorizationCode AuthCode { get; private set; }

        public WebpopForm()
        {
            InitializeComponent();
            Load += WebpopForm_Load;
        }

        public void WebpopForm_Load(object sender, EventArgs e)
        {
            browser.StartWebpop("sharefile.com", Globals.OAuthClientID, "onecitrix");
        }
    }
}
