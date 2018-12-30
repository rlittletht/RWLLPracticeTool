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
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Web;
using System.Web.SessionState;
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
        private HttpSessionState m_session;

        public delegate void LoginOutCallback(object sender, EventArgs e);

        LoginOutCallback m_onBeforeLogin;
        LoginOutCallback m_onAfterLogin;
        LoginOutCallback m_onBeforeLogout;
        LoginOutCallback m_onAfterLogout;

        /*----------------------------------------------------------------------------
        	%%Function: Auth
        	%%Qualified: Rwp.Auth.Auth
        	%%Contact: rlittle
        	
            Create a new auth object. This takes request, session, context, and 
            viewstate.

            to grab current privs or setup for logging in/out, call this.LoadPrivs
        ----------------------------------------------------------------------------*/
        public Auth(
            global::System.Web.UI.WebControls.ImageButton button, 
            HttpRequest request,
            HttpSessionState session,
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
            m_session = session;
        }

        [Serializable]
        public struct UserData
        {
            public UserPrivs privs;
            public string sIdentity;
            public string sTenant;
            public string sTeamName;
            public List<string> plsTeams;
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

        T TGetSessionState<T>(string sState, T tDefault)
        {
            T tValue = tDefault;

            if (m_session[sState] == null)
                m_session[sState] = tValue;
            else
                tValue = (T)m_session[sState];

            return tValue;
        }

        void SetSessionState<T>(string sState, T tValue)
        {
            m_session[sState] = tValue;
        }

        public Auth.UserData CurrentPrivs
        {
            get => TGetSessionState("privs", new Auth.UserData() { privs = Auth.UserPrivs.NotAuthenticated, sIdentity = null, sTeamName = null, sDivision = null });
            set => SetSessionState("privs", value);
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

        public string Tenant()
        {
            if (IsAuthenticated())
            {
                Regex rex = new Regex("https://login.microsoftonline.com/([^/]*)/");

                return rex.Match(System.Security.Claims.ClaimsPrincipal.Current.FindFirst("iss")?.Value).Groups[1].Value;
            }

            return null;
        }

        public static UserData EmptyAuth()
        {
            return new Auth.UserData { privs = Auth.UserPrivs.NotAuthenticated, sIdentity = null, sTeamName = null, sDivision = null, sTenant = null, plsTeams = null };
        }

        public void SetLoggedOff()
        {
            CurrentPrivs = new Auth.UserData { privs = Auth.UserPrivs.NotAuthenticated, sIdentity = null, sTeamName = null, sDivision = null, sTenant = null, plsTeams = null };
        }

        // they might be asking for a specific team (if they are switching who they are acting as)
        /*----------------------------------------------------------------------------
        	%%Function: LoadPrivs
        	%%Qualified: Rwp.Auth.LoadPrivs
        	%%Contact: rlittle
        	
            if not already authenticated, just return an empty UserData.

            if authenticated (and we have an access token), then load the current
            user privileges.

            This will query our auth tables to see if the currently authenticated
            user (and tenant) are authorized to access our data.

            * If not, then TeamName will be null on return. This means they don't get
              to access the tool or the data -- presumably the caller will present an
              error message
            * If yes, then return the team name (this might be a default from the list
              of authorized teams, or it might be the team name from the session cache
            
        ----------------------------------------------------------------------------*/
        public UserData LoadPrivs(SqlConnection DBConn, string sTeamNameSelected = null)
        {
            string sAuthIdentity = Identity();
            string sTenant = Tenant();
            bool fSoftTeamName = false;

            UserData data;

            data = CurrentPrivs;

            // before we reset the userdata, let's grab any already set teamname in the 
            // session state (this could have come from another page on our site)
            if (data.sTeamName != null)
            {
                if (sTeamNameSelected == null)
                {
                    sTeamNameSelected = data.sTeamName;
                    fSoftTeamName = true;
                }
            }

            data = new UserData()
            {
                sIdentity = null, privs = UserPrivs.NotAuthenticated, sTeamName = null, sDivision = null,
                sTenant = null, plsTeams = null
            };

            CurrentPrivs = data;

            if (sAuthIdentity == null || !IsAuthenticated())
                return data;

            string sqlStrLogin;

            data.sIdentity = sAuthIdentity;

            DBConn.Open();

            // first, get the list of teams we are authorized for...
            sqlStrLogin =
                $"SELECT PrimaryIdentity, Tenant, TeamID from rwllauth where PrimaryIdentity = '{Sql.Sqlify(sAuthIdentity)}' AND Tenant = '{Sql.Sqlify(sTenant)}'";

            SqlCommand cmdMbrs = DBConn.CreateCommand();
            cmdMbrs.CommandText = sqlStrLogin;
            SqlDataReader rdrMbrs = cmdMbrs.ExecuteReader();

            // there may be multiple returns, get them all
            // the last one will be the default (unless some form of "Admin" is there, then the last one of those will be the default,
            // or if they requested a specific team name
            while (rdrMbrs.Read())
            {
                if (data.plsTeams == null)
                    data.plsTeams = new List<string>();

                string sTeamName = rdrMbrs.GetString(2);
                data.plsTeams.Add(sTeamName);
                if (sTeamName.Contains("Admin") && sTeamNameSelected == null)
                {
                    data.sTeamName = sTeamName;
                }

                if (sTeamName == sTeamNameSelected)
                    data.sTeamName = sTeamName;
            }

            rdrMbrs.Close();
            cmdMbrs.Dispose();

            if (data.sTeamName == null)
            {
                if (sTeamNameSelected != null && !fSoftTeamName)
                {
                    // they are requesting a team that they aren't authorized to see
                    // (if the team name is soft, then this is just a previously cached
                    // value...they may have changed users)
                    throw new Exception("authorization failed");
                }

                if (data.plsTeams != null)
                    data.sTeamName = data.plsTeams[data.plsTeams.Count - 1]; // grab the last one as the default
            }

            if (data.sTeamName != null)
            {
                // don't need to validate a password -- once we have an authenticated identity, just get the privileges for the team name
                sqlStrLogin =
                    $"SELECT TeamName, Division from rwllTeams where TeamName = '{Sql.Sqlify(data.sTeamName)}'";
                cmdMbrs = DBConn.CreateCommand();
                cmdMbrs.CommandText = sqlStrLogin;
                rdrMbrs = cmdMbrs.ExecuteReader();

                while (rdrMbrs.Read())
                {
                    data.sTeamName = rdrMbrs.GetString(0);
                    data.sDivision = rdrMbrs.GetString(1);
                }

                rdrMbrs.Close();
                cmdMbrs.Dispose();
            }

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