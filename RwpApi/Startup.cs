using Microsoft.Owin;
using Owin;
using System.Configuration;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security.OAuth;
using Microsoft.IdentityModel.Tokens;

[assembly: OwinStartup(typeof(RwpApi.Startup))]

namespace RwpApi
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Invoke the ConfigureAuth method, which will set up
            // the OWIN authentication pipeline using OAuth 2.0

            ConfigureAuth(app);
        }

        public void ConfigureServices()
        {
        }

        public static string clientId = ConfigurationManager.AppSettings["ClientId"];

        #if PRODDATA
        public static string _sResourceConnString = ConfigurationManager.AppSettings["Thetasoft.Azure.ConnectionString"];
        #elif STAGEDATA
        public static string _sResourceConnString = ConfigurationManager.AppSettings["Thetasoft.Staging.Azure.ConnectionString"];
        #else
        public static string _sResourceConnString = ConfigurationManager.AppSettings["Thetasoft.Local.ConnectionString"];
        #endif

        public void ConfigureAuth(IAppBuilder app)
        {
            // NOTE: The usual WindowsAzureActiveDirectoryBearerAuthentication middleware uses a
            // metadata endpoint which is not supported by the v2.0 endpoint.  Instead, this 
            // OpenIdConnectSecurityTokenProvider implementation can be used to fetch & use the OpenIdConnect
            // metadata document - which for the v2 endpoint is https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration


            TokenValidationParameters tvps = new TokenValidationParameters
            {
                // This is where you specify that your API only accepts tokens from its own clients
                ValidAudiences = new[] {clientId },

                // Change below to 'true' if you want this Web API to accept tokens issued to one Azure AD tenant only (single-tenant)
                ValidateIssuer = true,
            };

            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions
            {
                AccessTokenFormat = new JwtFormat(
                    tvps,
                    new OpenIdConnectCachingSecurityTokenProvider(
                        "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration")
                )

            });
        }
    }
}