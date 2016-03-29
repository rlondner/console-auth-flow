#Okta Authentication Flow C# Sample

This sample demonstrates how to use Okta's Authentication and Session APIs through the most common [transaction state models](http://developer.okta.com/docs/api/resources/authn.html#transaction-state).

Specifically, you will learn when to expect a state token vs. a session token and what to do with them depending on the state you're in.

##Configuration
Follow the steps below to set up the sample:  
1. Edit the app.config file and uncomment the 4 lines in the `appSettings` section. 
2. Create an Okta developer organization at [http://developer.okta.com](http://developer.okta.com).  
3. Paste the url of your Okta developer organization into the `OktaTenantUrl` of your app.config file
3. Go to Admin -> Security -> API and press the "Create Token" button to create an API token
4. Copy the API Token and paste it into the `OktaApiKey` key of your app.config file.  
5. You may optionally fill out the `OktaUserLogin` and `OktaUserPassword` values if you want the sample to use these pre-configured values. Otherwise, you will be prompted to enter a user name and password at runtime.

##Testing various flows
Though this sample does not implement all the possible authentication flows the Okta platform enables, it should allow you to test a good variety of them. Namely, we recommend that you test the following configurations:  

  *  Enable second-factor authentication with Okta Verify, Google Authenticator and SMS Authentication  
  * Try setting up your organization to force passwords to expire (in Security -> General) or to be about to expire (in order to prompt the "Password Expiration Warning" notification)  
 

