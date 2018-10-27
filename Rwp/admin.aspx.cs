using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
// TODO NOTE:  We have a bit of a mess with all the different AzureStaging and Local, etc.
// we at least have a way to bind to both, but I don't thin there is any consistency
// about when the javascript redirects to the local machine and when the codebehind has bound
// to the local service.  very dangerous.  (already led to wiping out production data once).

// also, at some point, deploying to azure is going to get grumpy because we have two service
// endpoints bound (i think). this is why we have to figure out how to only define the one that
// we want in the conditional web.config files (look at smoking love to see how this is really supposed to be
// done...)

#if LOCALSERVICE
using RwpSvcProxy = Rwp.RwpSvcLocal;
#else
using RwpSvcProxy = Rwp.RwpSvc;
#endif

using System.Net;

namespace Rwp
{
    public partial class AdminPage : System.Web.UI.Page
    {
        private RwpSvcProxy.PracticeClient m_rspClient;
        private Auth m_auth;
        private SqlConnection DBConn;
        private Auth.UserPrivs m_userPrivs;

        protected void Page_Load(object sender, EventArgs e)
        {
            m_auth = new Auth(LoginOutButton, Request, null, null, null, null);

            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings["dbSchedule"];
            string sSqlConnectionString = conn.ConnectionString;

            DBConn = new SqlConnection(sSqlConnectionString);

            string sIdentity = System.Security.Claims.ClaimsPrincipal.Current.FindFirst("preferred_username")?.Value;

            m_userPrivs = m_auth.LoadPrivs(DBConn, sIdentity);
            ipClient.InnerText = Request.UserHostAddress;

            m_rspClient = new RwpSvcProxy.PracticeClient("BasicHttpBinding_Practice");
            EnableUIForAdmin();

            m_auth.SetupLoginLogout(Request.IsAuthenticated);
        }

        private void ReportSr(RwpSvcProxy.RSR sr, string sOperation)
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

        RwpSvcProxy.RSR CheckAdmin()
        {
            RwpSvcProxy.RSR sr = new RwpSvcProxy.RSR();
            string sAddressForComp = GetIP4Address(Request.UserHostAddress);

            if (m_userPrivs != Auth.UserPrivs.AdminPrivs)
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
            bool fAdmin = m_userPrivs == Auth.UserPrivs.AdminPrivs;

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
            RwpSvcProxy.RSR sr = CheckAdmin();

            if (!sr.Result)
                {
                ReportSr(sr, "ipc");
                return;
                }
    		// first download the current data...
            sr = m_rspClient.ClearSlots();
            ReportSr(sr, "Delete All Slots");
        }

		protected void DoDelete2014Slots(object sender, EventArgs e)
		{
            RwpSvcProxy.RSR sr = CheckAdmin();

            if (!sr.Result)
                {
                ReportSr(sr, "ipc");
                return;
                }
			// first download the current data...
			sr = m_rspClient.ClearYear(2014);
			ReportSr(sr, "Delete 2014 Slots");
		}

		protected void DoDeleteTeams(object sender, EventArgs e)
		{
            RwpSvcProxy.RSR sr = CheckAdmin();

		    if (!sr.Result)
		        {
		        ReportSr(sr, "ipc");
		        return;
		        }
		    // first download the current data...
			sr = m_rspClient.ClearTeams();
			ReportSr(sr, "Delete Teams");
		}

        protected void DoUploadTeams(object sender, EventArgs e)
        {
            RwpSvcProxy.RSR sr = CheckAdmin();

            if (!sr.Result)
                {
                ReportSr(sr, "ipc");
                return;
                }
            RwpSvcProxy.PracticeClient rspClientStream = new RwpSvcProxy.PracticeClient("BasicHttpBinding_PracticeStream");

            if ((fuTeams.PostedFile != null) && (fuTeams.PostedFile.ContentLength > 0))
                {

                System.Guid guid = System.Guid.NewGuid();

                string sAsPosted = System.IO.Path.GetFileName(fuTeams.PostedFile.FileName);
                string sUpload = Server.MapPath("\\Data") + "\\" + guid.ToString();

                sr = rspClientStream.ImportCsvTeams(fuTeams.PostedFile.InputStream);
                }
            else
                {
                sr = new RwpSvcProxy.RSR();
                sr.Result = false;
                sr.Reason = String.Format("Upload of file failed!");
                }
            ReportSr(sr, "Upload Teams");
            rspClientStream.Close();
        }

        protected void DoUploadSlots(object sender, EventArgs e)
        {
            RwpSvcProxy.RSR sr = CheckAdmin();

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

                sr = rspClientStream.ImportCsvSlots(fuSlots.PostedFile.InputStream);
                }
            else
                {
                sr = new RwpSvcProxy.RSR();
                sr.Result = false;
                sr.Reason = String.Format("Upload of file failed!");
                }
            ReportSr(sr, "Upload Slots");
            rspClientStream.Close();
        }

        protected void EnableClearItems(object sender, EventArgs e)
        {
            RwpSvcProxy.RSR sr = CheckAdmin();

            if (!sr.Result)
                {
                ReportSr(sr, "ipc");
                return;
                }

            btnClearTeams.Enabled = true;
        }

		protected void EnableDeleteSlots(object sender, EventArgs e)
		{
            RwpSvcProxy.RSR sr = CheckAdmin();

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