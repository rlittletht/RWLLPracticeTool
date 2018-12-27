using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Rwp
{
    public partial class icsfeed : System.Web.UI.Page
    {
        private ApiInterop m_apiInterop;

        protected void Page_Load(object sender, EventArgs e)
        {
            string sTeam = Request.QueryString["Team"];

            if (sTeam == null)
                {
                ErrorResponse.InnerText = "You must specify a Team in your request";
                return;
                }

            m_apiInterop = new ApiInterop(Context, Server);

            DoReport(sTeam);
        }

        protected void DoReport(string sTeam)
        {
            sTeam = sTeam.Replace(" ", "%20");

            RSR_CalItems rci = m_apiInterop.CallService<RSR_CalItems>($"http://localhost/rwpapi/api/slot/GetCalendarForTeam/{sTeam}", false);
            if (!rci.Result)
                {
                ErrorResponse.InnerText = String.Format("Failed to get calendar for {0}: {1}", sTeam, rci.Reason);
                return;
                }

            Response.Clear();
            Response.ContentType = "text/calendar";
            Response.AddHeader("Content-Disposition", "filename=\"practices.ics\"");

            Response.Write("BEGIN:VCALENDAR\r\n");
            Response.Write("PRODID://Thetasoft//RWLL Practice Tool\r\n");
            Response.Write("VERSION:2.0\r\n");

            foreach (CalItem ci in rci.TheValue)
                {
                Response.Write("BEGIN:VEVENT\r\n");
                Response.Write(String.Format("UID:{0}\r\n", ci.UID));
                Response.Write(String.Format("DTSTART:{0}\r\n", ci.Start.ToString("yyyyMMddTHHmmssZ")));
                Response.Write(String.Format("DTEND:{0}\r\n", ci.End.ToString("yyyyMMddTHHmmssZ")));
                Response.Write(String.Format("DESCRIPTION:{0}\r\n", ci.Description));
                Response.Write(String.Format("LOCATION:{0}\r\n", ci.Location));
                Response.Write(String.Format("SUMMARY:{0}\r\n", ci.Title));

                Response.Write("END:VEVENT\r\n");
                }

            Response.Write("END:VCALENDAR\r\n");
            Response.Flush();
            Response.End();
        }
    }
}