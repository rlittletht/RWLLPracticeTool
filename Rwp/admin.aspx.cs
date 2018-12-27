using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

namespace Rwp
{
    public partial class AdminPage : System.Web.UI.Page
    {
        static string s_sRoot = "/rwp";
        private Auth m_auth;
        private SqlConnection DBConn;
        private Auth.UserData m_userData;
        private ApiInterop m_apiInterop;

        static string ExtractServerNameFromConnection(string sConnection)
        {
            Regex rex = new Regex(".*server[ \t]*=[ \t]*([^;]*)[ \t]*;.*", RegexOptions.IgnoreCase);

            Match m = rex.Match(sConnection);

            return m.Groups[1].Value;
        }

        /*----------------------------------------------------------------------------
        	%%Function: CheckServiceServerConsistency
        	%%Qualified: Rwp.AdminPage.CheckServiceServerConsistency
        	%%Contact: rlittle
        	
            verify that the web client is consistent with the web api (the web client
            access the SQL server as well as the web api -- it sucks when they
            don't agree on the SQL server to be working with)
        ----------------------------------------------------------------------------*/
        void CheckServiceServerConsistency(string sSqlConnectionString)
        {
            if (!m_auth.IsLoggedIn)
                return;

            ServerInfo si;

            si = m_apiInterop.CallService<ServerInfo>("api/core/GetServerInfo", false);

            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings["dbSchedule"];

            string sSqlServer = ExtractServerNameFromConnection(sSqlConnectionString);
            if (String.Compare(si.sSqlServerHash, sSqlServer, true) != 0)
                throw new Exception($"SQL SERVER MISMATCH: {si.sSqlServerHash} != {sSqlServer}");
        }

        /*----------------------------------------------------------------------------
        	%%Function: Page_Load
        	%%Qualified: Rwp.AdminPage.Page_Load
        	%%Contact: rlittle
        	
            Initialize authentication and api interop
        ----------------------------------------------------------------------------*/
        protected void Page_Load(object sender, EventArgs e)
        {
            m_auth = new Auth(LoginOutButton, Request, Context.GetOwinContext().Environment["System.Web.HttpContextBase"] as HttpContextBase, ViewState, $"{s_sRoot}/admin.aspx", null, null, null, null);
            m_apiInterop = new ApiInterop(Context, Server, Startup.apiRoot);

            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings["dbSchedule"];
            string sSqlConnectionString = conn.ConnectionString;

            DBConn = new SqlConnection(sSqlConnectionString);

            m_userData = m_auth.LoadPrivs(DBConn);
            ipClient.InnerText = Request.UserHostAddress;

            CheckServiceServerConsistency(sSqlConnectionString);
            EnableUIForAdmin();

            m_auth.SetupLoginLogout();
        }

        /*----------------------------------------------------------------------------
        	%%Function: ReportSr
        	%%Qualified: Rwp.AdminPage.ReportSr
        	%%Contact: rlittle
        	
            report the SR to the web page
        ----------------------------------------------------------------------------*/
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

        RSR CheckAdmin()
        {
            RSR sr = new RSR();

            if (m_userData.privs != Auth.UserPrivs.AdminPrivs)
            {
                sr.Result = false;
                sr.Reason = $"User does not have administrative privileges";

                return sr;
            }

            sr.Result = true;
            return sr;
        }

        void EnableUIForAdmin()
        {
            bool fAdmin = m_userData.privs == Auth.UserPrivs.AdminPrivs;

            btnDownloadTeams.Enabled = fAdmin;
            btnClearTeams.Enabled = fAdmin;
            btnUploadTeams.Enabled = fAdmin;
            btnDownloadSlots.Enabled = fAdmin;
            btnClearAllSlots.Enabled = fAdmin;
            btnClearLastYear.Enabled = fAdmin;
            btnUploadSlots.Enabled = fAdmin;
        }

        /* D O  D E L E T E  S L O T S */
        /*----------------------------------------------------------------------------
        	%%Function: DoDeleteSlots
        	%%Qualified: Rwp.AdminPage.DoDeleteSlots
        	%%Contact: rlittle

        ----------------------------------------------------------------------------*/
        protected void DoDeleteSlots(object sender, EventArgs e)
        {
            RSR sr = CheckAdmin();

            if (!sr.Result)
            {
                ReportSr(sr, "ipc");
                return;
            }

            sr = m_apiInterop.CallService<RSR>("api/slot/DeleteAllSlots", true);

            ReportSr(sr, "Delete All Slots");
        }

		/*----------------------------------------------------------------------------
			%%Function: DoDelete2014Slots
			%%Qualified: Rwp.AdminPage.DoDelete2014Slots
			
            Intended to be changed every year, currently hardcoded to 2014 (will be
            2018 once the 2019 season starts)
		----------------------------------------------------------------------------*/
		protected void DoDelete2014Slots(object sender, EventArgs e)
		{
		    RSR sr = CheckAdmin();

		    if (!sr.Result)
		    {
		        ReportSr(sr, "ipc");
		        return;
		    }

		    sr = m_apiInterop.CallService<RSR>($"api/slot/DeleteSlotsByYear/2014", true);
			ReportSr(sr, "Delete 2014 Slots");
		}

		/*----------------------------------------------------------------------------
			%%Function: DoDeleteTeams
			%%Qualified: Rwp.AdminPage.DoDeleteTeams
			%%Contact: rlittle
			
		----------------------------------------------------------------------------*/
		protected void DoDeleteTeams(object sender, EventArgs e)
		{
            RSR sr = CheckAdmin();

		    if (!sr.Result)
		        {
		        ReportSr(sr, "ipc");
		        return;
		        }
		    // first download the current data...
		    sr = m_apiInterop.CallService<RSR>("api/team/DeleteTeams", true);
			ReportSr(sr, "Delete Teams");
		}

        /*----------------------------------------------------------------------------
        	%%Function: DoUploadTeams
        	%%Qualified: Rwp.AdminPage.DoUploadTeams
        	%%Contact: rlittle
        	
            upload a csv file of teams to the server
        ----------------------------------------------------------------------------*/
        protected void DoUploadTeams(object sender, EventArgs e)
        {
            RSR sr;

            if ((fuTeams.PostedFile != null) && (fuTeams.PostedFile.ContentLength > 0))
            {
                HttpContent content = new StreamContent(fuTeams.PostedFile.InputStream);

                sr = m_apiInterop.CallServicePut<RSR>("api/team/PutTeams", content, true);
            }
            else
            {
                sr = new RSR();
                sr.Result = false;
                sr.Reason = String.Format("Upload of file failed!");
                }
            ReportSr(sr, "Upload Teams");
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoUploadSlots
        	%%Qualified: Rwp.AdminPage.DoUploadSlots
        	%%Contact: rlittle
        	
            upload a csv file of slots to the server
        ----------------------------------------------------------------------------*/
        protected void DoUploadSlots(object sender, EventArgs e)
        {
            RSR sr;

            if ((fuSlots.PostedFile != null) && (fuSlots.PostedFile.ContentLength > 0))
            {
                HttpContent content = new StreamContent(fuSlots.PostedFile.InputStream);

                sr = m_apiInterop.CallServicePut<RSR>("api/slot/PutSlots", content, true);
            }
            else
            {
                sr = new RSR();
                sr.Result = false;
                sr.Reason = String.Format("Upload of file failed!");
            }
            ReportSr(sr, "Upload Slots");
        }

        protected void EnableClearItems(object sender, EventArgs e)
        {
            RSR sr = CheckAdmin();

            if (!sr.Result)
                {
                ReportSr(sr, "ipc");
                return;
                }

            btnClearTeams.Enabled = true;
        }

		protected void EnableDeleteSlots(object sender, EventArgs e)
		{
            RSR sr = CheckAdmin();

		    if (!sr.Result)
		        {
		        ReportSr(sr, "ipc");
		        return;
		        }

		    btnClearAllSlots.Enabled = true;
			btnClearLastYear.Enabled = true;
		}
	}
}