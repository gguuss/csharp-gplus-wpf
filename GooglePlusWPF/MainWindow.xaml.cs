using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// Added for auth flow
// Microsoft
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

// Libraries for Google APIs
using Google.Apis.Authentication.OAuth2;
using Google.Apis.Authentication.OAuth2.DotNetOpenAuth;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.Plus.v1;
using Google.Apis.Plus.v1.Data;

// For OAuth2
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;


// For reading window titles
using System.Runtime.InteropServices;

namespace GooglePlusWPF
{

    /// <summary>
    /// Class for storing client configuration
    /// Create these from https://code.google.com/apis/console
    /// </summary>
    public static class ClientCredentials
    {
        static public string ClientID = "YOUR_CLIENT_ID";
        static public string ClientSecret = "YOUR_CLIENT_SECRET";
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Used for polling windows/reading the auth code from the window title
        [ DllImport("user32.dll") ] 
        static extern int GetForegroundWindow(); 
        [ DllImport("user32.dll") ]
        static extern int GetWindowText(int hWnd, StringBuilder text, int count); 

        
        /// <summary>
        /// The service object for API calls to the Google+ API
        /// </summary>
        protected PlusService plusService;
        /// <summary>
        /// The currently authorized Google+ user
        /// </summary>
        protected Person me;
        /// <summary>
        /// Stores the OAuth v2 state (access token, refresh token, expiration, etc...)
        /// </summary>
        private IAuthorizationState _authstate;

        /// <summary>
        /// WPF constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            me = null;
            _authstate = null;
        }

        private OAuth2Authenticator<NativeApplicationClient> CreateAuthenticator()
        {
            // Register the authenticator.
            var provider = new NativeApplicationClient(GoogleAuthenticationServer.Description);
            provider.ClientIdentifier = ClientCredentials.ClientID;
            provider.ClientSecret = ClientCredentials.ClientSecret;
                    
            var authenticator =
                new OAuth2Authenticator<NativeApplicationClient>(provider, GetAuthorization) { NoCaching = true };
            return authenticator;
        }

        /// <summary>
        /// Polls active window titles to try and find the authorization code based on the window
        /// string.
        /// </summary>
        /// <param name="sleepTime">The frequency used to poll, in milliseconds.</param>
        /// <returns>The authorization code.</returns>
        string PollActiveWindowForAuthCode(int sleepTime){
            string activeTitle = GetActiveWindowTitle();
            while (!activeTitle.StartsWith("Success"))
            {
                activeTitle = GetActiveWindowTitle();
                Thread.Sleep(sleepTime);
            }
            // strip to start of auth code
            string trimToAuthCode = activeTitle.Substring(activeTitle.LastIndexOf("=") + 1);
            // trim the " - Google Chrome" text
            return trimToAuthCode.Substring(0, trimToAuthCode.IndexOf(' '));
        }

        /// <summary>
        /// Helper for PollActiveWindowForAuthCode that retrieves the window string.
        /// </summary>
        /// <returns>The active window's title string.</returns>
        private string GetActiveWindowTitle() 
        { 
            const int nChars = 256; 
            IntPtr handle = IntPtr.Zero; 
            StringBuilder Buff = new StringBuilder(nChars); 
            handle = (IntPtr)GetForegroundWindow(); 
 
            if (GetWindowText((int)handle, Buff, nChars) > 0) 
            { 
                return Buff.ToString(); 
            } 
            return null; 
        } 

        /// <summary>
        /// Retrieves the authorization object by starting the OAuth v2 flow in a new web browser.
        /// </summary>
        /// <param name="client">The client used for performing the API calls.</param>
        /// <returns>An authorization state containing the data used for making API calls to
        /// Google+.</returns>
        private IAuthorizationState GetAuthorization(NativeApplicationClient client)
        {
            String[] reqAuthScopes = new[] { "https://www.googleapis.com/auth/plus.login"};

            // Generate the authstate
            if (_authstate == null)
            {
                // need an authorization state
                _authstate = new AuthorizationState(reqAuthScopes);


                // Create the Url
                Uri requestURL = client.RequestUserAuthorization(reqAuthScopes);

                Uri url = client.RequestUserAuthorization(reqAuthScopes);

                // Show the dialog
                Process.Start(url.ToString());

                // TODO: Do this better. You could close the window after done, you could track this
                // better and can avoid the busy wait
                //MessageBox.Show("Please click OK after you have copied the token from the web site.");
                String authCode = PollActiveWindowForAuthCode(50);
                //String authCode = Clipboard.GetDataObject().GetData(DataFormats.Text).ToString();

                if (string.IsNullOrEmpty(authCode))
                {
                    throw new Exception("The authentication request was cancelled by the user.");
                }

                IAuthorizationState state = client.ProcessUserAuthorization(
                        new Uri("http://localhost"), _authstate);
                return client.ProcessUserAuthorization(authCode, state);
            }

            // Now perform auth flow
            return null;
        }

        /// <summary>
        /// Authenticates a user.
        /// </summary>
        public void Authenticate(){
            // Register the authenticator.
            var auth = CreateAuthenticator();

            // Create the service.
            plusService = new PlusService(new BaseClientService.Initializer()
            {
                Authenticator = auth
            });
            if (plusService != null)
            {
                PeopleResource.GetRequest getReq = plusService.People.Get("me");

                Person me = getReq.Fetch();

                if (me != null)
                {
                    statusText.Text = "You successfully Authenticated!";
                    resultText.Text = "UserName: " + me.DisplayName + "\nURL: " + me.Url +
                        "\nProfile id: " + me.Id;
                }
            }
        }

        private void AuthButtonClick(object sender, RoutedEventArgs e)
        {
            Authenticate();
        }
    }
}
