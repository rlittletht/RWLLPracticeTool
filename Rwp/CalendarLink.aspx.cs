using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Rwp
{
    public partial class CalendarLink : System.Web.UI.Page
    {
#if PRODHOST
        static string s_sRoot = "";
        #else
        static string s_sRoot = "/rwp";
#endif
        private Auth m_auth;
        private ApiInterop m_apiInterop;
        private Auth.UserData m_userData;
        private SqlConnection DBConn;

        protected void Page_Load(object sender, EventArgs e)
        {
            m_auth = new Auth(LoginOutButton, Request, Session, Context.GetOwinContext().Environment["System.Web.HttpContextBase"] as HttpContextBase, ViewState, $"{s_sRoot}/CalendarLink.aspx", null, null, null, null);
            m_apiInterop = new ApiInterop(Context, Server, Startup.apiRoot);

            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings["dbSchedule"];
            string sSqlConnectionString = conn.ConnectionString;

            DBConn = new SqlConnection(sSqlConnectionString);

            m_userData = m_auth.LoadPrivs(DBConn);

            m_auth.SetupLoginLogout();

        }

        protected void DoCreateLink(object sender, EventArgs e)
        {
            string sComment = txtComment.Text;
            string sAuthority = m_auth.Identity();
            string sTeam = m_auth.CurrentPrivs.sTeamName;
            Guid guidLinkID = System.Guid.NewGuid();

        }
    }
}