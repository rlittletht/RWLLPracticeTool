using System;
using System.IdentityModel.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms.VisualStyles;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using TCore.MsalWeb;

[assembly: OwinStartup(typeof(Rwp.Startup))]

namespace Rwp
{
    public class Startup
    {
#if PRODHOST
        public static string s_sRoot = "";
        public static string s_sFullRoot = "https://rwllpractice.azurewebsites.net";
#elif STAGEHOST
        public static string s_sRoot = "/rwp";
        public static string s_sFullRoot = "https://thetasoft2.azurewebsites.net/rwp";
#else
        public static string s_sRoot = "/rwp";
        public static string s_sFullRoot = "http://localhost/rwp";
#endif
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        public static string clientId = System.Configuration.ConfigurationManager.AppSettings["ClientId"];

        // RedirectUri is the URL where the user will be redirected to after they sign in.
        public static string redirectUri = System.Configuration.ConfigurationManager.AppSettings["RedirectUri"];

        // The AppKey is used to create a ConfidentialClientApplication in order to exchange an auth token for an access token
        public static string appKey = System.Configuration.ConfigurationManager.AppSettings["AppKey"];

        // When requesting scopes during login (and token exchange), we need to specify that we want to access
        // our webapi, since this is a different application. We get this scope from the WebApi application registration page.
        public static string scopeWebApi = System.Configuration.ConfigurationManager.AppSettings["WebApiScope"];

        // Tenant is the tenant ID (e.g. contoso.onmicrosoft.com, or 'common' for multi-tenant)
        public static string tenant = System.Configuration.ConfigurationManager.AppSettings["Tenant"];

        // Authority is the URL for authority, composed by Azure Active Directory v2 endpoint and the tenant name (e.g. https://login.microsoftonline.com/contoso.onmicrosoft.com/v2.0)
        public static string authority = String.Format(System.Globalization.CultureInfo.InvariantCulture, System.Configuration.ConfigurationManager.AppSettings["Authority"], tenant);

        public static string apiRoot = System.Configuration.ConfigurationManager.AppSettings["WebApiRoot"];


        public void Configuration(IAppBuilder app)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            
            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    // Sets the ClientId, authority, RedirectUri as obtained from web.config
                    ClientId = clientId,
                    Authority = authority,
                    RedirectUri = redirectUri,
                    // PostLogoutRedirectUri is the page that users will be redirected to after sign-out. In this case, it is using the home page
                    PostLogoutRedirectUri = redirectUri,
                    Scope = $"{scopeWebApi} {OpenIdConnectScope.OpenIdProfile} offline_access user.read",
                    // ResponseType is set to request the id_token - which contains basic information about the signed-in user
                    ResponseType = OpenIdConnectResponseType.CodeIdToken,
                    // ValidateIssuer set to false to allow personal and work accounts from any organization to sign in to your application
                    // To only allow users from a single organizations, set ValidateIssuer to true and 'tenant' setting in web.config to the tenant name
                    // To allow users from only a list of specific organizations, set ValidateIssuer to true and use ValidIssuers parameter 
                    TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = false
                    },
                    // OpenIdConnectAuthenticationNotifications configures OWIN to send notification of failed authentications to OnAuthenticationFailed method
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        AuthenticationFailed = OnAuthenticationFailed,
                        AuthorizationCodeReceived = OnAuthorizationCodeReceived
                    }
                    
                }
            );
        }

        /// <summary>
        /// Handle failed authentication requests by redirecting the user to the home page with an error in the query string
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Task OnAuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context)
        {
            context.HandleResponse();
            context.Response.Redirect("/?errormessage=FAILED:" + context.Exception.Message);
            return Task.FromResult(0);
        }

        private async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedNotification context)
        {
            var code = context.Code;

            string signedInUserID = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;
            TokenCache userTokenCache = new MSALSessionCache(signedInUserID, context.OwinContext.Environment["System.Web.HttpContextBase"] as HttpContextBase).GetMsalCacheInstance();

            ConfidentialClientApplication cca =
                new ConfidentialClientApplication(clientId, redirectUri, new ClientCredential(appKey), userTokenCache, null);

            AuthenticationResult result = await cca.AcquireTokenByAuthorizationCodeAsync(code, new string[] { scopeWebApi });
            // the result doesn't matter -- our goal is to just populate the TokenCache, which the above call does.
        }
    }
}
