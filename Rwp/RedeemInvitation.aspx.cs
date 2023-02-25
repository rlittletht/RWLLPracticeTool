using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;
using TCore;

namespace Rwp
{
    public partial class RedeemInvitation : Page
    {
        private Auth m_auth;
        private ApiInterop m_apiInterop;
        private SqlConnection DBConn;
        private Auth.UserData m_userData;

        protected global::System.Web.UI.WebControls.ImageButton GoHome;
        protected global::System.Web.UI.WebControls.ImageButton LoginOutButton;
        protected global::System.Web.UI.WebControls.TextBox txtPrimaryIdentity;
        protected global::System.Web.UI.WebControls.TextBox txtTenantId;
        protected global::System.Web.UI.WebControls.TextBox txtInvitationCode;
        protected global::System.Web.UI.HtmlControls.HtmlGenericControl divError;

#if PRODHOST
        static string s_sRoot = "";
#else
        static string s_sRoot = "/rwp";
#endif
        protected void Page_Load(object sender, EventArgs e)
        {
            m_auth = new Auth(
                LoginOutButton,
                Request,
                Session,
                Context.GetOwinContext().Environment["System.Web.HttpContextBase"] as HttpContextBase,
                ViewState,
                $"{s_sRoot}/RedeemInvitation.aspx",
                null,
                null,
                null,
                null);

            m_apiInterop = new ApiInterop(Context, Server, Startup.apiRoot);

            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings["dbSchedule"];
            string sSqlConnectionString = conn.ConnectionString;

            DBConn = new SqlConnection(sSqlConnectionString);

            m_auth.SetupLoginLogout();
            GoHome.Click += DoGoHome;

            string primaryIdentity = Request.QueryString["PrimaryIdentity"];
            string tenant = Request.QueryString["Tenant"];

            if (!IsPostBack)
            {
                txtPrimaryIdentity.Text = primaryIdentity;
                txtTenantId.Text = tenant;
            }
        }


        public void DoGoHome(object sender, ImageClickEventArgs args)
        {
            Response.Redirect(Startup.s_sFullRoot);
        }

        private void ReportSr(RSR sr, string sOperation)
        {
            if (!sr.Result)
            {
                divError.Visible = true;
                divError.InnerText = sr.Reason;
            }
            else
            {
                divError.InnerText = String.Format("{0} returned no errors.", sOperation);
            }
        }

        public void DoRedeem(object sender, EventArgs args)
        {
            RSR sr;

            try
            {
                sr = m_apiInterop.CallService<RSR>(
                    $"api/team/RedeemInvitation?Identity={txtPrimaryIdentity.Text}&Tenant={txtTenantId.Text}&InvitationCode={txtInvitationCode.Text}",
                    false);
            }
            catch (Exception exc)
            {
                sr = new RSR();
                sr.Result = false;
                sr.Reason = exc.Message;
            }

            ReportSr(sr, "Redeem Invitation Code");
        }
    }
}
