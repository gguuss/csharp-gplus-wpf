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
// DotNetOpenAuth
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
//Google
using Google.Apis.Authentication.OAuth2;
using Google.Apis.Authentication.OAuth2.DotNetOpenAuth;
using Google.Apis.Util;
using System.Web;
// The generated plus class
using Plus.v1;
// For reading window titles
using System.Runtime.InteropServices;

namespace GooglePlusWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        
        [ DllImport("user32.dll") ] 
        static extern int GetForegroundWindow(); 
        [ DllImport("user32.dll") ]
        static extern int GetWindowText(int hWnd, StringBuilder text, int count); 

 

        protected PlusService ps1;
        Plus.v1.Data.Person me;
        private IAuthorizationState _authstate;

        public MainWindow()
        {
            InitializeComponent();
            me = null;
            _authstate = null;
        }

        private static class ClientCredentials
        {
            static public string ClientID = "YOUR_CLIENT_ID";
            static public string ClientSecret = "YOUR_CLIENT_SECRET";
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

        string pollActiveWindowForAuthCode(int sleepTime){
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

                // Show the dialog.
                Process.Start(url.ToString());

                // TODO:  Watch the windows for the auth code from within there
                //MessageBox.Show("Please click OK after you have copied the token from the web site.");
                String authCode = pollActiveWindowForAuthCode(50);
                //String authCode = Clipboard.GetDataObject().GetData(DataFormats.Text).ToString();

                if (string.IsNullOrEmpty(authCode))
                {
                    throw new Exception("The authentication request was cancelled by the user.");                
                }                

                IAuthorizationState state = client.ProcessUserAuthorization(new Uri("http://localhost"), _authstate);
                return client.ProcessUserAuthorization(authCode, state);                
            }

            // Now perform auth flow
            return null;
        }        

        public void authenticate(){
            // Register the authenticator.                        
            var auth = CreateAuthenticator();

            // Create the service.            
            ps1 = new PlusService(auth);
            if (ps1 != null)
            {
                PeopleResource.GetRequest grrrr = ps1.People.Get("me");

                Plus.v1.Data.Person me = grrrr.Fetch();

                if (me != null)
                {
                    textBlock1.Text = "You successfully Authenticated!";                    
                    textBlock2.Text = "UserName: " + me.DisplayName + "\nURL: " + me.Url + "\nProfile id: " + me.Id;
                }
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {            
            authenticate();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {            

            if (me != null)
            {
                // buy gus a beer
            }            
        }
    }
}
