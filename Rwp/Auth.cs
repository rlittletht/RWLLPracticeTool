using System;
using Microsoft.Owin;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.EnterpriseServices;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security.Claims;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Web;
using Microsoft.Identity.Client;
using Microsoft.Owin.Security.Notifications;
using Owin;
using TCore;

namespace Rwp
{
    public partial class Auth
    {
        private global::System.Web.UI.WebControls.ImageButton m_buttonLoginOut;
        private HttpRequest m_request;
        private string m_sAuthReturnAddress;
        private HttpContextBase m_context;
        private StateBag m_viewState;

        public delegate void LoginOutCallback(object sender, EventArgs e);

        LoginOutCallback m_onBeforeLogin;
        LoginOutCallback m_onAfterLogin;
        LoginOutCallback m_onBeforeLogout;
        LoginOutCallback m_onAfterLogout;

        public Auth(
            global::System.Web.UI.WebControls.ImageButton button, 
            HttpRequest request,
            HttpContextBase context,
            StateBag viewState,
            string sReturnAddress,
            LoginOutCallback onBeforeLogin,
            LoginOutCallback onAfterLogin,
            LoginOutCallback onBeforeLogout,
            LoginOutCallback onAfterLogout)
        {
            m_sAuthReturnAddress = sReturnAddress;
            m_buttonLoginOut = button;
            m_request = request;
            m_onBeforeLogin = onBeforeLogin;
            m_onAfterLogin = onAfterLogin;
            m_onBeforeLogout = onBeforeLogout;
            m_onAfterLogout = onAfterLogout;
            m_context = context;
            m_viewState = viewState;
        }

        [Serializable]
        public struct UserData
        {
            public UserPrivs privs;
            public string sIdentity;
            public string sTeamName;
            public string sDivision;
        }

        [Serializable]
        public enum UserPrivs
        {
            NotAuthenticated,
            AuthenticatedNoPrivs,
            UserPrivs,
            AdminPrivs
        };

        T TGetState<T>(string sState, T tDefault)
        {
            T tValue = tDefault;

            if (m_viewState[sState] == null)
                m_viewState[sState] = tValue;
            else
                tValue = (T)m_viewState[sState];

            return tValue;
        }

        void SetState<T>(string sState, T tValue)
        {
            m_viewState[sState] = tValue;
        }

        public Auth.UserData CurrentPrivs
        {
            get => TGetState("privs", new Auth.UserData() { privs = Auth.UserPrivs.NotAuthenticated, sIdentity = null, sTeamName = null, sDivision = null });
            set => SetState("privs", value);
        }

        public bool IsLoggedIn  => CurrentPrivs.privs != Auth.UserPrivs.NotAuthenticated && CurrentPrivs.privs != Auth.UserPrivs.AuthenticatedNoPrivs;

        public bool IsAuthenticated()
        {
            return IsSignedIn();
        }

        public string Identity()
        {
            if (IsAuthenticated())
                return System.Security.Claims.ClaimsPrincipal.Current.FindFirst("preferred_username")?.Value;

            return null;
        }

        public void SetLoggedOff()
        {
            CurrentPrivs = new Auth.UserData { privs = Auth.UserPrivs.NotAuthenticated, sIdentity = null, sTeamName = null, sDivision = null};
        }

        public UserData LoadPrivs(SqlConnection DBConn)
        {
            string sAuthIdentity = Identity();

            UserData data = new UserData() {sIdentity = null, privs = UserPrivs.NotAuthenticated, sTeamName = null, sDivision = null};
            CurrentPrivs = data;

            if (sAuthIdentity == null || !IsAuthenticated())
                return data;

            string sqlStrLogin;

            data.sIdentity = sAuthIdentity;

            DBConn.Open();
            // don't need to validate a password -- once we have an authenticated identity, just get its privileges
            sqlStrLogin = $"SELECT TeamName, Division from rwllTeams where Email1 = '{Sql.Sqlify(sAuthIdentity)}'";
            SqlCommand cmdMbrs = DBConn.CreateCommand();
            cmdMbrs.CommandText = sqlStrLogin;
            SqlDataReader rdrMbrs = cmdMbrs.ExecuteReader();
            
            while (rdrMbrs.Read())
            {
                data.sTeamName = rdrMbrs.GetString(0);
                data.sDivision = rdrMbrs.GetString(1);
            }

            rdrMbrs.Close();
            cmdMbrs.Dispose();
            DBConn.Close();


            if (string.IsNullOrEmpty(data.sTeamName))
            {
                data.privs = UserPrivs.AuthenticatedNoPrivs;
                CurrentPrivs = data;
                return data;
            }

            if (data.sDivision == "X")
            {
                data.privs = UserPrivs.AdminPrivs;
                CurrentPrivs = data;
                return data;
            }

            data.privs = UserPrivs.UserPrivs;
            CurrentPrivs = data;
            return data;
        }

        public void DoSignInClick(object sender, ImageClickEventArgs args)
        {
            m_onBeforeLogin?.Invoke(sender, args);
            SignIn(m_request.IsAuthenticated, m_sAuthReturnAddress);
            m_onAfterLogin?.Invoke(sender, args);
        }

        public void DoSignOutClick(object sender, ImageClickEventArgs args)
        {
            m_onBeforeLogout?.Invoke(sender, args);
            SignOut();
            m_onAfterLogout?.Invoke(sender, args);
        }

        public void SetupLoginLogout()
        {
            if (IsAuthenticated())
            {
                m_buttonLoginOut.Click -= DoSignInClick;
                m_buttonLoginOut.Click += DoSignOutClick;
                m_buttonLoginOut.ImageUrl = "signout.png";
            }
            else
            {
                m_buttonLoginOut.Click -= DoSignOutClick;
                m_buttonLoginOut.Click += DoSignInClick;
                m_buttonLoginOut.ImageUrl = "signin.png";
            }
        }

        /// <summary>
        /// Send an OpenID Connect sign-in request.
        /// Alternatively, you can just decorate the SignIn method with the [Authorize] attribute
        /// </summary>
        public void SignIn(bool fIsAuthenticated, string sReturnAddress)
        {
            if (!IsAuthenticated())
            {
                HttpContext.Current.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = sReturnAddress},
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }

        /// <summary>
        /// Send an OpenID Connect sign-out request.
        /// </summary>
        public void SignOut()
        {
            HttpContext.Current.GetOwinContext().Authentication.SignOut(
                OpenIdConnectAuthenticationDefaults.AuthenticationType,
                CookieAuthenticationDefaults.AuthenticationType);
        }

        protected void ValidateLogin(bool fIsAuthenticated)
        {
            SignIn(IsAuthenticated(), m_sAuthReturnAddress);
        }


        /*----------------------------------------------------------------------------
        	%%Function: GetUserId
        	%%Qualified: WebApp._default.GetUserId
        	
            convenient way to get the current user id (so we can get to the right
            TokenCache)
        ----------------------------------------------------------------------------*/
        string GetUserId()
        {
            return ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
        }

        /*----------------------------------------------------------------------------
        	%%Function: IsSignedIn
        	%%Qualified: WebApp._default.IsSignedIn
        	
            return true if the signin process is complete -- this includes making 
            sure there is an entry for this userid in the TokenCache
        ----------------------------------------------------------------------------*/
        bool IsSignedIn()
        {
            return m_request.IsAuthenticated && FTokenCachePopulated();
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetAccessToken
        	%%Qualified: WebApp._default.GetAccessToken

            Get an access token for accessing the WebApi. This will use 
            AcquireTokenSilentAsync to get the token. Since this is using the 
            same tokencache as we populated when the user logged in, we will
            get the access token from that cache. 
        ----------------------------------------------------------------------------*/
        string GetAccessToken()
        {
            if (!IsSignedIn())
                return null;

            // Retrieve the token with the specified scopes
            var scopes = new string[] {Startup.scopeWebApi};
            string userId = GetUserId();
            TokenCache tokenCache = new MSALSessionCache(userId, m_context).GetMsalCacheInstance();
            ConfidentialClientApplication cca = new ConfidentialClientApplication(Startup.clientId, Startup.authority, Startup.redirectUri, new ClientCredential(Startup.appKey), tokenCache, null);

            Task<IEnumerable<IAccount>> tskAccounts = cca.GetAccountsAsync();
            tskAccounts.Wait();

            IAccount account = tskAccounts.Result.FirstOrDefault();

            Task<AuthenticationResult> tskResult = cca.AcquireTokenSilentAsync(scopes, account, Startup.authority, false);

            tskResult.Wait();
            return tskResult.Result.AccessToken;
        }

        /*----------------------------------------------------------------------------
        	%%Function: FTokenCachePopulated
        	%%Qualified: WebApp._default.FTokenCachePopulated
        	
        	return true if our TokenCache has been populated for the current 
            UserId.  Since our TokenCache is currently only stored in the session, 
            if our session ever gets reset, we might get into a state where there
            is a cookie for auth (and will let us automatically login), but the
            TokenCache never got populated (since it is only populated during the
            actual authentication process). If this is the case, we need to treat
            this as if the user weren't logged in. The user will SignIn again, 
            populating the TokenCache.
        ----------------------------------------------------------------------------*/
        bool FTokenCachePopulated()
        {
            return MSALSessionCache.CacheExists(GetUserId(), m_context);
        }
    }
}