﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rwp.RwpSvc;

namespace Rwp
{
    public partial class AdminPage : System.Web.UI.Page
    {
        private RwpSvc.PracticeClient m_rspClient;

        protected void Page_Load(object sender, EventArgs e)
        {
            ipClient.InnerText = Request.UserHostAddress;

            m_rspClient = new PracticeClient("BasicHttpBinding_Practice");
        }

        private void ReportSr(RwpSvc.SR sr, string sOperation)
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

        SR CheckIP()
        {
            SR sr = new SR();

            if (String.Compare(Request.UserHostAddress, "50.135.16.218") != 0
                && String.Compare(Request.UserHostAddress, "::1") != 0
                && !Request.UserHostAddress.StartsWith("192.168.1."))
                {
                sr.Result = false;
                sr.Reason = String.Format("admin operations illegal from current ip address: {0}",
                                          Request.UserHostAddress);
                return sr;
                }

            sr.Result = true;
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
            SR sr = CheckIP();

            if (!sr.Result)
                {
                ReportSr(sr, "ipc");
                return;
                }
    		// first download the current data...
            sr = m_rspClient.ClearSlots();
            ReportSr(sr, "Delete All Slots");
        }

		protected void DoDelete2012Slots(object sender, EventArgs e)
		{
            SR sr = CheckIP();

            if (!sr.Result)
                {
                ReportSr(sr, "ipc");
                return;
                }
			// first download the current data...
			sr = m_rspClient.ClearYear(2012);
			ReportSr(sr, "Delete 2012 Slots");
		}

		protected void DoDeleteTeams(object sender, EventArgs e)
		{
            SR sr = CheckIP();

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
            SR sr = CheckIP();

            if (!sr.Result)
                {
                ReportSr(sr, "ipc");
                return;
                }
            RwpSvc.PracticeClient rspClientStream = new PracticeClient("BasicHttpBinding_PracticeStream");

            if ((fuTeams.PostedFile != null) && (fuTeams.PostedFile.ContentLength > 0))
                {

                System.Guid guid = System.Guid.NewGuid();

                string sAsPosted = System.IO.Path.GetFileName(fuTeams.PostedFile.FileName);
                string sUpload = Server.MapPath("\\Data") + "\\" + guid.ToString();

                sr = rspClientStream.ImportCsvTeams(fuTeams.PostedFile.InputStream);
                }
            else
                {
                sr = new SR();
                sr.Result = false;
                sr.Reason = String.Format("Upload of file failed!");
                }
            ReportSr(sr, "Upload Teams");
            rspClientStream.Close();
        }

        protected void DoUploadSlots(object sender, EventArgs e)
        {
            SR sr = CheckIP();

            if (!sr.Result)
                {
                ReportSr(sr, "ipc");
                return;
                }
            RwpSvc.PracticeClient rspClientStream = new PracticeClient("BasicHttpBinding_PracticeStream");

            if ((fuSlots.PostedFile != null && fuSlots.PostedFile.ContentLength > 0))
                {
                System.Guid guid = System.Guid.NewGuid();

                string sAsPosted = System.IO.Path.GetFileName(fuSlots.PostedFile.FileName);
                string sUpload = Server.MapPath("\\Data") + "\\" + guid.ToString();

                sr = rspClientStream.ImportCsvSlots(fuSlots.PostedFile.InputStream);
                }
            else
                {
                sr = new SR();
                sr.Result = false;
                sr.Reason = String.Format("Upload of file failed!");
                }
            ReportSr(sr, "Upload Slots");
            rspClientStream.Close();
        }

        protected void EnableClearItems(object sender, EventArgs e)
        {
            SR sr = CheckIP();

            if (!sr.Result)
                {
                ReportSr(sr, "ipc");
                return;
                }

            btnClearTeams.Enabled = true;
        }

		protected void EnableDeleteSlots(object sender, EventArgs e)
		{
            SR sr = CheckIP();

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