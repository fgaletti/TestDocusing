using System;
using static DocuSign.eSign.Client.Auth.OAuth;
using System.Configuration;
using DocuSign.eSign.Client;
using System.Runtime.InteropServices;
using System.Diagnostics;
using static DocuSign.eSign.Client.Auth.OAuth.UserInfo;
using System.Linq;
using System.IO;
using DocuSign.eSign.Model;
using DocuSign.eSign.Api;
using static DocuSign.eSign.Api.EnvelopesApi;

namespace TestDocusign
{
    class Program
    {
        static readonly string DevCenterPage = "https://developers.docusign.com/platform/auth/consent";
      //  static readonly string DevCenterPage = "http://localhost:8080";

        static void Main(string[] args)
        {
            OAuthToken accessToken = null;
            Console.WriteLine("Hello World!");

            try
            {
                accessToken = JWTAuth.AuthenticateWithJWT("ESignature", ConfigurationManager.AppSettings["ClientId"], ConfigurationManager.AppSettings["ImpersonatedUserId"],
                                                            ConfigurationManager.AppSettings["AuthServer"], ConfigurationManager.AppSettings["PrivateKeyFile"]);
            }
            catch(ApiException apiExp)
            {
                if (apiExp.Message.Contains("consent_required"))
                {
                    // Caret needed for escaping & in windows URL
                    string caret = "";
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        caret = "^";
                    }

                    // build a URL to provide consent for this Integration Key and this userId
                    string url = "https://" + ConfigurationManager.AppSettings["AuthServer"] + "/oauth/auth?response_type=code" + caret + "&scope=impersonation%20signature" + caret +
                        "&client_id=" + ConfigurationManager.AppSettings["ClientId"] + caret + "&redirect_uri=" + DevCenterPage;
                    Console.WriteLine($"Consent is required - launching browser (URL is {url})");

                    // Start new browser window for login and consent to this app by DocuSign user
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = false });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", url);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", url);
                    }

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to send envelope; Exiting. Please rerun the console app once consent was provided");
                    Console.ForegroundColor = ConsoleColor.White;
                    Environment.Exit(-1);
                }
            }



            var apiClient = new ApiClient();
            apiClient.SetOAuthBasePath(ConfigurationManager.AppSettings["AuthServer"]);
            DocuSign.eSign.Client.Auth.OAuth.UserInfo userInfo = apiClient.GetUserInfo(accessToken.access_token);
            Account acct = userInfo.Accounts.FirstOrDefault();
            // ---- List ----
            //string basePath = "https://demo.docusign.net/restapi";
            //string accountId = "5a2be190-141c-4cbe-b698-8c7a713c443c";

            //var listDocuments = DoWork(accessToken.access_token, basePath, accountId);

            //foreach (var item in listDocuments.Envelopes)
            //{
            //    Console.WriteLine($"  {item.EnvelopeId} - {item.LastModifiedDateTime} -  {item.Status}");
            //}

            // Send 
            Console.WriteLine("Welcome to the JWT Code example! ");
            Console.Write("Enter the signer's email address: ");
            string signerEmail = Console.ReadLine();
            Console.Write("Enter the signer's name: ");
            string signerName = Console.ReadLine();
            Console.Write("Enter the carbon copy's email address: ");
            string ccEmail = Console.ReadLine();
            Console.Write("Enter the carbon copy's name: ");
            string ccName = Console.ReadLine();
            //string docDocx = Path.Combine(@"..", "..", "..", "..", "launcher-csharp", "World_Wide_Corp_salary.docx");
            //string docPdf = Path.Combine(@"..", "..", "..", "..", "launcher-csharp", "World_Wide_Corp_lorem.pdf");
            string docDocx = Path.Combine(@"World_Wide_Corp_salary.docx");
            string docPdf = Path.Combine(@"World_Wide_Corp_lorem.pdf");
            Console.WriteLine("");
            string envelopeId = SigningViaEmail.SendEnvelopeViaEmail(signerEmail, signerName, ccEmail, ccName, accessToken.access_token, acct.BaseUri + "/restapi", acct.AccountId, docDocx, docPdf, "sent");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Successfully sent envelope with envelopeId {envelopeId}");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.White;
            Environment.Exit(0);
        }

        private static EnvelopesInformation DoWork(string accessToken, string basePath, string accountId)
        {
            // Data for this method
            // accessToken
            // basePath
            // accountId

            var apiClient = new ApiClient(basePath);
            apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken);
            var envelopesApi = new EnvelopesApi(apiClient);
            ListStatusChangesOptions options = new ListStatusChangesOptions();
            options.fromDate = DateTime.Now.AddDays(-30).ToString("yyyy/MM/dd");
            // Call the API method:
            EnvelopesInformation results = envelopesApi.ListStatusChanges(accountId, options);
            return results;
        }
    }
}
