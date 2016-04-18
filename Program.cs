using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Okta.Core;
using Okta.Core.Clients;
using Okta.Core.Models;
using System.Web;
using System.Configuration;

namespace Okta.Samples
{
    class Program
    {
        static string oktaUserLogin = string.Empty;
        static string oktaUserPassword = string.Empty;
        static string oktaTenantUrl = string.Empty;
        static string oktaApiKey = string.Empty;
        static AuthClient authClient = null;
        static OktaClient oktaClient = null;
        static UsersClient usersClient = null;
        static void Main(string[] args)
        {
            try
            {
                AuthFlow();
                Console.WriteLine("Thank you for trying out this code sample. Please press Enter to exit.");
                Console.ReadKey();

            }
            catch (OktaAuthenticationException oAE)
            {
                Console.Write(oAE.ToString());
                Console.ReadKey();
            }
            catch (OktaException ex)
            {
                Console.Write(ex.ToString());
                Console.WriteLine(ex.ErrorCode + ":" + ex.ErrorSummary);
                Console.ReadKey();
            }
        }
        public static void AuthFlow()
        {
            oktaTenantUrl = ConfigurationManager.AppSettings["OktaTenantUrl"];
            oktaApiKey = ConfigurationManager.AppSettings["OktaApiKey"];
            oktaUserLogin = ConfigurationManager.AppSettings["OktaUserLogin"];
            oktaUserPassword = ConfigurationManager.AppSettings["OktaPassword"];

            //A valid Api Key is NOT necessary when using the Authentication Client
            authClient = new AuthClient(new OktaSettings
            {
                BaseUri = new Uri(oktaTenantUrl),
                ApiToken = ""
            });

            //A valid Api Key IS necessary when using the generic Okta Client (a convenience client to create other clients)
            oktaClient = new OktaClient(oktaApiKey, new Uri(oktaTenantUrl));
            usersClient = oktaClient.GetUsersClient();

            try
            {
                if (string.IsNullOrEmpty(oktaUserLogin) || string.IsNullOrEmpty(oktaUserPassword))
                {
                    Console.Write("Please enter your username and press Enter: ");
                    oktaUserLogin = Console.ReadLine();
                    
                    Console.Write("Please enter your password and press Enter: ");
                    ConsoleKeyInfo key;
                    do
                    {
                        key = Console.ReadKey(true);
                        // Backspace Should Not Work
                        if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                        {
                            oktaUserPassword += key.KeyChar;
                            Console.Write("*");
                        }
                        else
                        {
                            if (key.Key == ConsoleKey.Backspace && oktaUserPassword.Length > 0)
                            {
                                oktaUserPassword = oktaUserPassword.Substring(0, (oktaUserPassword.Length - 1));
                                Console.Write("\b \b");
                            }
                        }
                    }
                    // Stops Receiving Keys Once Enter is Pressed
                    while (key.Key != ConsoleKey.Enter);
                    Console.WriteLine();
                }

                var authRes = authClient.Authenticate(oktaUserLogin, oktaUserPassword, null, true, false);

                var strAuthStatus = authRes.Status;
                var stateToken = authRes.StateToken; //only populated if the user has to go through some additional authentication steps, such as second factor authentication
                var sessionToken = authRes.SessionToken; //only populated if the authentication response is SUCCESS

                ProcessResponse(authRes);

            }
            catch (OktaAuthenticationException oAE)
            {
                switch (oAE.ErrorCode)
                {
                    case OktaErrorCodes.AuthenticationException:
                        Console.WriteLine("You entered a wrong login/password combination, please press Enter and try again");
                        Console.ReadLine();
                        break;
                    default:
                        break;
                }
                //Console.Write(oAE.ToString());
                //Console.ReadKey();
            }
            catch (OktaException ex)
            {
                string strCustomMessage = string.Empty;
                switch (ex.ErrorCode)
                {
                    case OktaErrorCodes.TooManyRequestsException:
                        strCustomMessage = "Please try to decrease your authentication rate to less than 1 per second";
                        break;
                    default:
                        break;
                }

                Console.Write(ex.ToString());
                Console.WriteLine(ex.ErrorCode + ":" + ex.ErrorSummary);
                Console.ReadKey();
            }

            // OktaClient is a convenience client to create other clients
            //var oktaClient = new OktaClient(apiKey, new Uri(tenantUrl));


            //UsersClient userClient = oktaClient.GetUsersClient();
            //User user = userClient.Get(oktaUserLogin);



        }

        private static void ProcessResponse(AuthResponse authResponse)
        {
            switch (authResponse.Status)
            {
                case AuthStatus.PasswordExpired:
                    Console.WriteLine("Your password has expired, you must update it now");
                    AuthResponse res = ChangePassword(authResponse);
                    //AuthNewPassword authNewPwd = new AuthNewPassword();
                    //authNewPwd.OldPassword = oktaUserPassword;
                    //authNewPwd.NewPassword = "pass";
                    //Uri uri = new Uri(oktaTenantUrl + "/api/v1/authn/credentials/change_password");
                    //AuthResponse res = authClient.Execute(authResponse.StateToken, uri, authNewPwd);
                    ProcessResponse(res);
                    break;

                case AuthStatus.MfaRequired:
                    List<Factor> userFactors = authResponse.Embedded.Factors;
                    Console.WriteLine("You must sign with a second factor. Please press the number next to the factor of your choice:");
                    int factorNumber = 0;
                    foreach (Factor f in userFactors)
                    {
                        Console.WriteLine("[{0}] {1} {2}", ++factorNumber, f.FactorType, f.Provider);
                    }
                    string strFactorNumber = Console.ReadLine();
                    //need more error processing here
                    int iFactorNumber = Int32.Parse(strFactorNumber);
                    Factor selectedFactor = userFactors[iFactorNumber - 1];

                    User user = usersClient.Get(oktaUserLogin);
                    UserFactorsClient ufc = usersClient.GetUserFactorsClient(user);
                    ChallengeResponse mfaRes = null;

                    while (mfaRes == null || mfaRes.FactorResult != "SUCCESS")
                    {
                        switch (selectedFactor.FactorType)
                        {
                            case "token:software:totp":
                                Console.WriteLine("Please enter your Google Authenticator code");
                                string strCode = Console.ReadLine();
                                //MfaAnswer answer = new MfaAnswer
                                //{
                                //    Passcode = strCode
                                //};
                                try
                                {
                                    authResponse = authClient.VerifyTotpFactor(selectedFactor.Id, authResponse, strCode);
                                    //mfaRes = ufc.CompleteChallenge(selectedFactor, answer);
                                }
                                catch (OktaException oe)
                                {
                                    if (oe.ErrorCode == OktaErrorCodes.FactorInvalidCodeException)
                                    {
                                        Console.WriteLine("You entered an invalid code");
                                    }
                                }
                                break;
                            case "push":
                                Console.WriteLine("Please accept the request from Okta Verify on your phone");
                                //ChallengeResponse bc = ufc.BeginChallenge(selectedFactor);

                                //MfaAnswer answer = new MfaAnswer();

                                string factorResult = string.Empty;
                                //Uri pollTransactionUri = null;
                                //if (bc != null && (factorResult = bc.FactorResult) == "WAITING")
                                //{
                                //    pollTransactionUri = bc.Links["poll"][0].Href;
                                //    //System.Threading.Thread.Sleep(500);
                                //}

                                while (factorResult == string.Empty || factorResult == "WAITING" || factorResult == "MFA_CHALLENGE")
                                {
                                    //mfaRes = ufc.PollTransaction(pollTransactionUri.ToString());
                                    authResponse = authClient.VerifyPullFactor(selectedFactor.Id, authResponse);
                                    factorResult = authResponse.FactorResult;
                                    System.Threading.Thread.Sleep(500);
                                    Console.WriteLine("Waiting for user to confirm the push notification...");
                                }

                                break;
                            case "sms":
                                //ChallengeResponse sms = ufc.BeginChallenge(selectedFactor);
                                Console.WriteLine("Please type the code you just received on your phone:");
                                strCode = Console.ReadLine();
                                //MfaAnswer answer = new MfaAnswer
                                //{
                                //    Passcode = strCode
                                //};
                                //try
                                //{
                                //    mfaRes = ufc.CompleteChallenge(selectedFactor, answer);
                                //}
                                try
                                {
                                    authResponse = authClient.VerifyTotpFactor(selectedFactor.Id, authResponse, strCode);
                                    //mfaRes = ufc.CompleteChallenge(selectedFactor, answer);
                                }
                                catch (OktaException oe)
                                {
                                    if (oe.ErrorCode == OktaErrorCodes.FactorInvalidCodeException)
                                    {
                                        Console.WriteLine("You entered an invalid code");
                                    }
                                }
                                break;


                            default:
                                break;
                        }

                        if (authResponse != null && authResponse.Status == "SUCCESS")
                        {
                            Console.WriteLine("Your MFA response was successful");
                            ProcessResponse(authResponse);
                        }
                        else
                        {
                            Console.WriteLine("Please try again");
                        }
                    }

                    break;

                case AuthStatus.PasswordWarn:
                    Console.WriteLine("Your password is expiring soon. Would you like to reset your password now? Press Y for 'yes', No for 'no' (default: No)");
                    string strPasswordWarnResponse = Console.ReadLine();
                    if (strPasswordWarnResponse == "Y")
                    {
                        authResponse = ChangePassword(authResponse);
                        ProcessResponse(authResponse);
                    }
                    else
                    {
                        authResponse = authClient.Skip(authResponse.StateToken);
                        ProcessResponse(authResponse);
                    }
                    break;

                case AuthStatus.Success:

                    Console.WriteLine("Your session token is the following: " + authResponse.SessionToken);

                    SessionsClient sessionsClient = new SessionsClient(new OktaSettings
                    {
                        BaseUri = new Uri(oktaTenantUrl),
                        ApiToken = oktaApiKey
                    });

                    //you must exchange your session token for a real session (default duration: 2 hours, but can be changed in the Sign On Policy) if you want to perform other operations on behalf of the user (such as performing an Single-Sign-On operation)
                    Session session = sessionsClient.CreateSession(authResponse.SessionToken);

                    session = sessionsClient.Validate(session);
                    try
                    {
                        session = sessionsClient.Extend(session.Id);
                    }
                    catch (Exception ex)
                    {
                        string s = ex.ToString() ;
                    }
                    sessionsClient.Close(session);
                    //try
                    //{
                    //    sessionsClient.Validate(session);
                    //}
                    //catch (OktaException e)
                    //{
                    //    string strError = e.ErrorSummary;
                    //}

                    break;
                default:
                    break;
            }

        }

        private static AuthResponse ChangePassword(AuthResponse authResponse) {
            string strNewPassword = "";
            string strNewPasswordRepeat = "";
            bool bSamePassword = false;

            while (!bSamePassword)
            {
                Console.Write("Please enter your new password:");
                ConsoleKeyInfo key;
                do
                {
                    key = Console.ReadKey(true);
                    // Backspace Should Not Work
                    if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                    {
                        strNewPassword += key.KeyChar;
                        Console.Write("*");
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.Backspace && strNewPassword.Length > 0)
                        {
                            strNewPassword = strNewPassword.Substring(0, (strNewPassword.Length - 1));
                            Console.Write("\b \b");
                        }
                    }
                }
                // Stops Receiving Keys Once Enter is Pressed
                while (key.Key != ConsoleKey.Enter);
                Console.WriteLine();
                Console.WriteLine("Please enter your new password again:");
                do
                {
                    key = Console.ReadKey(true);
                    // Backspace Should Not Work
                    if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                    {
                        strNewPasswordRepeat += key.KeyChar;
                        Console.Write("*");
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.Backspace && strNewPasswordRepeat.Length > 0)
                        {
                            strNewPasswordRepeat = strNewPasswordRepeat.Substring(0, (strNewPasswordRepeat.Length - 1));
                            Console.Write("\b \b");
                        }
                    }
                }
                // Stops Receiving Keys Once Enter is Pressed
                while (key.Key != ConsoleKey.Enter);
                Console.WriteLine();

                if (strNewPassword == strNewPasswordRepeat)
                    bSamePassword = true;
                else
                    Console.WriteLine("You didn't enter the same password, please try again.");
            }

            AuthResponse response = authClient.ChangePassword(authResponse.StateToken, oktaUserPassword, strNewPassword);
            return response;

        }
    }
}