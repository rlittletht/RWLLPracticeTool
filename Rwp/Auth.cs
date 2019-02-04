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
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Owin.Security.Notifications;
using Owin;
using TCore;
using TCore.MsalWeb;

namespace Rwp
{
    public class RwpAuth : IAuthClient<RwpAuth.UserData>
    {
        private TCore.MsalWeb.Auth<UserData> m_auth;

        private global::System.Web.UI.WebControls.ImageButton m_buttonLoginOut;
        public delegate void LoginOutCallback(object sender, EventArgs e);

        LoginOutCallback m_onBeforeLogin;
        LoginOutCallback m_onBeforeLogout;

        public void BeforeLogin(object sender, EventArgs e)
        {
            m_onBeforeLogin?.Invoke(sender, e);
        }

        public void BeforeLogout(object sender, EventArgs e)
        {
            m_onBeforeLogout?.Invoke(sender, e);
        }

        /*----------------------------------------------------------------------------
        	%%Function: Auth
        	%%Qualified: Rwp.Auth.Auth
        	%%Contact: rlittle
        	
            Create a new auth object. This takes request, session, context, and 
            viewstate.

            to grab current privs or setup for logging in/out, call this.LoadPrivs
        ----------------------------------------------------------------------------*/
        public RwpAuth(
            global::System.Web.UI.WebControls.ImageButton button, 
            HttpRequest request,
            HttpSessionState session,
            HttpContextBase context,
            StateBag viewState,
            string sReturnAddress,
            LoginOutCallback onBeforeLogin,
            LoginOutCallback onBeforeLogout)
        {
            m_auth = new Auth<UserData>(request, session, context, context.GetOwinContext(), viewState, sReturnAddress, this);
            m_buttonLoginOut = button;
            m_onBeforeLogin = onBeforeLogin;
            m_onBeforeLogout = onBeforeLogout;
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

        public UserData CreateEmpty()
        {
            return EmptyAuth();
        }

        public bool AuthIsAuthenticated() => m_auth.CurrentPrivs.privs != RwpAuth.UserPrivs.NotAuthenticated;

        public bool AuthHasPrivileges() => m_auth.CurrentPrivs.privs != RwpAuth.UserPrivs.NotAuthenticated &&
                                           m_auth.CurrentPrivs.privs != RwpAuth.UserPrivs.AuthenticatedNoPrivs;

        public bool IsLoggedIn => m_auth.IsLoggedIn;

        public UserData CurrentPrivs
        {
            get => m_auth.CurrentPrivs;
            set => m_auth.CurrentPrivs = value;
        }
        public bool IsAuthenticated()
        {
            return m_auth.IsSignedIn();
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

        public void SetAuthenticated(bool fAuthenticated)
        {
            if (fAuthenticated)
            {
                UserData data = m_auth.CurrentPrivs;
                data.privs = UserPrivs.AuthenticatedNoPrivs;
                m_auth.CurrentPrivs = data;
            }
            else
                m_auth.CurrentPrivs = EmptyAuth();

        }
        public static UserData EmptyAuth()
        {
            return new RwpAuth.UserData { privs = RwpAuth.UserPrivs.NotAuthenticated, sIdentity = null, sTeamName = null, sDivision = null, sTenant = null, plsTeams = null };
        }

        public void SetLoggedOff()
        {
            CurrentPrivs = new RwpAuth.UserData { privs = RwpAuth.UserPrivs.NotAuthenticated, sIdentity = null, sTeamName = null, sDivision = null, sTenant = null, plsTeams = null };
        }

        void GetTeamListAndDefaultForQuery(SqlConnection DBConn, string sqlStrLogin, string sTeamNameSelected, out List<string> plsTeams, out string sTeam, out string sAdminTeam)
        {
            sTeam = null;
            plsTeams = null;
            sAdminTeam = null;

            SqlCommand cmdMbrs = DBConn.CreateCommand();
            cmdMbrs.CommandText = sqlStrLogin;
            SqlDataReader rdrMbrs = cmdMbrs.ExecuteReader();

            // there may be multiple returns, get them all
            // the last one will be the default (unless some form of "Admin" is there, then the last one of those will be the default,
            // or if they requested a specific team name
            while (rdrMbrs.Read())
            {
                if (plsTeams == null)
                    plsTeams = new List<string>();

                string sTeamName = rdrMbrs.GetString(0);
                plsTeams.Add(sTeamName);

                // admin is the default unless there is a preferred team name.
                // (even if there is a preferred team name, we will still return an "admin team" name
                // so the caller knows we are an admin)
                if (sTeamName.Contains("Admin"))
                {
                    sAdminTeam = sTeamName;

                    if (sTeamNameSelected == null)
                        sTeam = sTeamName;
                }

                if (sTeamName == sTeamNameSelected)
                    sTeam = sTeamName;
            }

            rdrMbrs.Close();
            rdrMbrs.Dispose();
            cmdMbrs.Dispose();
        }

                
        void GetTeamListAndDefault(SqlConnection DBConn, string sAuthIdentity, string sTenant, string sTeamNameSelected, out List<string> plsTeams, out string sTeam, out string sAdminTeam)
        {
            string sqlStrLogin =
                $"SELECT TeamID from rwllauth where PrimaryIdentity = '{Sql.Sqlify(sAuthIdentity)}' AND Tenant = '{Sql.Sqlify(sTenant)}'";

            GetTeamListAndDefaultForQuery(DBConn, sqlStrLogin, sTeamNameSelected, out plsTeams, out sTeam, out sAdminTeam);
        }

        void LoadPrivsForTeam(SqlConnection DBConn, string sTeamNameIn, out string sTeamName, out string sDivision)
        {
            sTeamName = null;
            sDivision = null;

            // don't need to validate a password -- once we have an authenticated identity, just get the privileges for the team name
            string sqlStrLogin =
                $"SELECT TeamName, Division from rwllTeams where TeamName = '{Sql.Sqlify(sTeamNameIn)}'";

            SqlCommand cmdMbrs = DBConn.CreateCommand();
            cmdMbrs.CommandText = sqlStrLogin;
            SqlDataReader rdrMbrs = cmdMbrs.ExecuteReader();

            while (rdrMbrs.Read())
            {
                sTeamName = rdrMbrs.GetString(0);
                sDivision = rdrMbrs.GetString(1);
            }

            rdrMbrs.Close();
            rdrMbrs.Dispose();
            cmdMbrs.Dispose();
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

            data.sIdentity = sAuthIdentity;

            DBConn.Open();

            // first, get the list of teams we are authorized for...
            // (we might have a requested team name, if so, use that as the default)
            // (if we are an admin, then we won't get all the teams here...which means
            // the requested team name might not be found...yet.  if we are admin
            // then we will be authorized for all teams, and we will load that list
            // later
            string sAdminTeam;

            GetTeamListAndDefault(DBConn, sAuthIdentity, sTenant, sTeamNameSelected, out data.plsTeams, out data.sTeamName, out sAdminTeam);

            if (sAdminTeam != null || (data.sTeamName != null && data.sTeamName.Contains("Admin")))
            {
                // let's make sure its really an admin -- the division has to be "X" for this admin team
                // (yes, there's a weird case where our identity is associated with multiple teams with "Admin" in
                // the name, but the last one we get (which because the default one), doesn't have "X" in its 
                // division name, which means its really not an admin. this means we won't get admin
                // privileges. The only way to really get the admin privileges is to selected the correct
                // admin team name in the list of team names, then we will load the right privs. of course, then
                // when you choose the team you want to act as, we will lose that real admin selection and we will 
                // again go to the "last one wins" default, and we will lost our admin privileges).
                // 
                // the moral of this store? don't put "Admin" in a team name if you aren't going to put it in
                // division "X" to make it a real admin. unless you are trying to drive someone crazy
                string sTeamOut, sDivision;

                string sTeamIn = data.sTeamName;

                sTeamIn = sAdminTeam;

                if (data.sTeamName != null && data.sTeamName.Contains("Admin"))
                    sTeamIn = data.sTeamName;
                   
                LoadPrivsForTeam(DBConn, sTeamIn, out sTeamOut, out sDivision);

                if (sDivision == "X")
                {
                    // this is an admin. load the list of all teams for our list of teams
                    string sqlStrLogin = $"SELECT TeamName from rwllTeams";

                    GetTeamListAndDefaultForQuery(DBConn, sqlStrLogin, sTeamNameSelected, out data.plsTeams, out data.sTeamName, out sAdminTeam);
                }
            }

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
                LoadPrivsForTeam(DBConn, data.sTeamName, out data.sTeamName, out data.sDivision);
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
            m_auth.SignIn(sender, args);
        }

        public void DoSignOutClick(object sender, ImageClickEventArgs args)
        {
            m_auth.SignOut(sender, args);
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
    }
}