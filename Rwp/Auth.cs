using System;
using Microsoft.Owin;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.EnterpriseServices;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Web;
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

        public delegate void LoginOutCallback(object sender, EventArgs e);

        LoginOutCallback m_onBeforeLogin;
        LoginOutCallback m_onAfterLogin;
        LoginOutCallback m_onBeforeLogout;
        LoginOutCallback m_onAfterLogout;

        public Auth(
            global::System.Web.UI.WebControls.ImageButton button, 
            HttpRequest request,
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
        }

        public enum UserPrivs
        {
            NotAuthenticated,
            AuthenticatedNoPrivs,
            UserPrivs,
            AdminPrivs
        };

        public UserPrivs LoadPrivs(SqlConnection DBConn, string sIdentity)
        {
            if (sIdentity == null)
                return UserPrivs.NotAuthenticated;

            string sqlStrLogin;

            DBConn.Open();
            // don't need to validate a password -- once we have an authenticated identity, just get its privileges
            sqlStrLogin = $"SELECT TeamName as Count from rwllTeams where Email1 = '{Sql.Sqlify(sIdentity)}'";
            SqlCommand cmdMbrs = DBConn.CreateCommand();
            cmdMbrs.CommandText = sqlStrLogin;
            SqlDataReader rdrMbrs = cmdMbrs.ExecuteReader();
            string teamName = null;

            while (rdrMbrs.Read())
            {
                teamName = rdrMbrs.GetString(0);
            }

            rdrMbrs.Close();
            cmdMbrs.Dispose();
            DBConn.Close();

            if (teamName == null)
                return UserPrivs.AuthenticatedNoPrivs;

            if (teamName == "Administrator")
                return UserPrivs.AdminPrivs;

            return UserPrivs.UserPrivs;
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

        public void SetupLoginLogout(bool fIsAuthenticated)
        {
            if (fIsAuthenticated)
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
        public void SignIn(bool IsAuthenticated, string sReturnAddress)
        {
            if (!IsAuthenticated)
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

        protected void ValidateLogin(bool IsAuthenticated)
        {
            SignIn(IsAuthenticated, m_sAuthReturnAddress);
        }

    }
}