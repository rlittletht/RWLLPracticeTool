using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

#if LOCALSVC
using RwpSvcProxy = Rwp.RwpSvcLocal;
#elif STAGESVC
using RwpSvcProxy = Rwp.RwpSvcStaging;
#elif PRODSVC
using RwpSvcProxy = Rwp.RwpSvc;
#else
#error "No service endpoint defined"
#endif

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
        private RwpSvcProxy.PracticeClient m_rspClient;
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

        void CheckServiceServerConsistency(string sSqlConnectionString)
        {
            return; 
            RwpSvcProxy.ServerInfo si;

            si = m_rspClient.GetServerInfo();

            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings["dbSchedule"];

            string sSqlServer = ExtractServerNameFromConnection(sSqlConnectionString);
            if (String.Compare(si.sSqlServerHash, sSqlServer, true) != 0)
                throw new Exception($"SQL SERVER MISMATCH: {si.sSqlServerHash} != {sSqlServer}");
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            m_auth = new Auth(LoginOutButton, Request, Context.GetOwinContext().Environment["System.Web.HttpContextBase"] as HttpContextBase, ViewState, $"{s_sRoot}/admin.aspx", null, null, null, null);
            m_apiInterop = new ApiInterop(Context, Server);

            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings["dbSchedule"];
            string sSqlConnectionString = conn.ConnectionString;

            DBConn = new SqlConnection(sSqlConnectionString);

            m_userData = m_auth.LoadPrivs(DBConn);
            ipClient.InnerText = Request.UserHostAddress;

            m_rspClient = new RwpSvcProxy.PracticeClient("BasicHttpBinding_Practice");
            CheckServiceServerConsistency(sSqlConnectionString);
            EnableUIForAdmin();

            m_auth.SetupLoginLogout();
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

        public static string GetIP4Address(string sUserHostAddress)
        {
            string IP4Address = String.Empty;

            foreach (IPAddress IPA in Dns.GetHostAddresses(sUserHostAddress))
            {
                if (IPA.AddressFamily.ToString() == "InterNetwork")
                {
                    IP4Address = IPA.ToString();
                    break;
                }
            }

            if (IP4Address != String.Empty)
            {
                return IP4Address;
            }

            foreach (IPAddress IPA in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (IPA.AddressFamily.ToString() == "InterNetwork")
                {
                    IP4Address = IPA.ToString();
                    break;
                }
            }

            return IP4Address;
        }

        RSR CheckAdmin()
        {
            RSR sr = new RSR();
            string sAddressForComp = GetIP4Address(Request.UserHostAddress);

            if (m_userData.privs != Auth.UserPrivs.AdminPrivs)
            {
                sr.Result = false;
                sr.Reason = $"User does not have administrative privileges";

                return sr;
            }

            if (String.Compare(sAddressForComp, "73.83.16.112") != 0
                && String.Compare(sAddressForComp, "::1") != 0
                && !sAddressForComp.StartsWith("192.168.1."))
                {
                sr.Result = false;
                sr.Reason = String.Format("Admin operations illegal from current ip address: {0}",
                                          sAddressForComp);
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

        RSR RsrFromRsr(RwpSvcProxy.RSR rsr)
        {
            RSR sr = new RSR();

            sr.Result = rsr.Result;
            sr.Reason = rsr.Reason;

            return sr;
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
    		// first download the current data...
            sr = RsrFromRsr(m_rspClient.ClearSlots());
            ReportSr(sr, "Delete All Slots");
        }

		protected void DoDelete2014Slots(object sender, EventArgs e)
		{
            RSR sr = CheckAdmin();

            if (!sr.Result)
                {
                ReportSr(sr, "ipc");
                return;
                }
			// first download the current data...
			sr = RsrFromRsr(m_rspClient.ClearYear(2014));
			ReportSr(sr, "Delete 2014 Slots");
		}

		protected void DoDeleteTeams(object sender, EventArgs e)
		{
            RSR sr = CheckAdmin();

		    if (!sr.Result)
		        {
		        ReportSr(sr, "ipc");
		        return;
		        }
		    // first download the current data...
			sr = RsrFromRsr(m_rspClient.ClearTeams());
			ReportSr(sr, "Delete Teams");
		}

        protected void DoUploadTeams(object sender, EventArgs e)
        {
            RSR sr;


            if ((fuTeams.PostedFile != null) && (fuTeams.PostedFile.ContentLength > 0))
            {
                HttpContent content = new StreamContent(fuTeams.PostedFile.InputStream);

                sr = m_apiInterop.CallServicePut<RSR>("http://localhost/rwpapi/api/team/PutTeams", content, false);
            }
            else
            {
                sr = new RSR();
                sr.Result = false;
                sr.Reason = String.Format("Upload of file failed!");
                }
            ReportSr(sr, "Upload Teams");
        }

        protected void DoUploadSlots(object sender, EventArgs e)
        {
            RSR sr = CheckAdmin();

            if (!sr.Result)
                {
                ReportSr(sr, "ipc");
                return;
                }
            RwpSvcProxy.PracticeClient rspClientStream = new RwpSvcProxy.PracticeClient("BasicHttpBinding_PracticeStream");

            if ((fuSlots.PostedFile != null && fuSlots.PostedFile.ContentLength > 0))
                {
                System.Guid guid = System.Guid.NewGuid();

                string sAsPosted = System.IO.Path.GetFileName(fuSlots.PostedFile.FileName);
                string sUpload = Server.MapPath("\\Data") + "\\" + guid.ToString();

                sr = RsrFromRsr(rspClientStream.ImportCsvSlots(fuSlots.PostedFile.InputStream));
                }
            else
                {
                sr = new RSR();
                sr.Result = false;
                sr.Reason = String.Format("Upload of file failed!");
                }
            ReportSr(sr, "Upload Slots");
            rspClientStream.Close();
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