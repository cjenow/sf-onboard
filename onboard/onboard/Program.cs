using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShareFile.Api.Client;
using ShareFile.Api.Client.Extensions;
using ShareFile.Api.Client.Security.Authentication.OAuth2;

namespace ShareFile.Onboard.UI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ChooseDirectory());
        } 

        public static IShareFileClient GetZachApiClient()
        {
            string username = "zachariah.jeyakaran@citrix.com";
            string password = "Citrix123";
            string subdomain = "jeffcombscom";

            string baseUri = String.Format("https://{0}.{1}/sf/v3", subdomain, "sf-api.com");
            var api = new InternalShareFileClient(baseUri);
            var oauthService = new OAuthService(api, Globals.OAuthClientID, Globals.OAuthClientSecret);
            var token = oauthService.GetPasswordGrantRequestQuery(username, password, subdomain, "sharefile.com").Execute();
            api.AddOAuthCredentials(new Uri(baseUri), token.AccessToken);

            return api;
        }
    }
}
