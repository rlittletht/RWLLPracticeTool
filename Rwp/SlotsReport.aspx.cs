using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rwp.RwpSvc;

namespace Rwp
{
    public partial class SlotsReport : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            DoReport();
        }


        private RSR CheckIP()
        {
            RSR sr = new RSR();

            if (String.Compare(Request.UserHostAddress, "73.83.16.112") != 0
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

        protected void DoReport()
        {
            if (!CheckIP().Result)
                return;

            PracticeClient rspClient = new PracticeClient("BasicHttpBinding_PracticeStream");

            Stream stm = rspClient.GetCsvSlots();
            TextReader tr = new StreamReader(stm);

            Response.Clear();
            Response.ContentType = "text/csv";
            Response.AddHeader("Content-Disposition", "filename=\"Slots.csv\"");

            string sLine;

            while ((sLine = tr.ReadLine()) != null)
                {
                Response.Write(sLine);
                Response.Write("\n");
                }

            //    	    Response.Write(s);
            Response.Write("\n");
            Response.Flush();
            Response.End();
        }
    }
}