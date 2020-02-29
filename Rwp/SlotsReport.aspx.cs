using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Rwp
{
    public partial class SlotsReport : System.Web.UI.Page
    {
        private ApiInterop m_apiInterop;

        protected void Page_Load(object sender, EventArgs e)
        {
            m_apiInterop = new ApiInterop(Context, Server, Startup.apiRoot);

            DoReport();
        }

        protected void DoReport()
        {
            // future: wire up timezone info properly. for now hardcode PST
            HttpResponseMessage resp = m_apiInterop.CallService("api/slot/GetSlots?TimeZoneID=Pacific%20Standard%20Time", true);
            Task<Stream> tskStream = resp.Content.ReadAsStreamAsync();
            tskStream.Wait();

            Stream stm = tskStream.Result;
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

            Response.Write("\n");
            Response.Flush();
            Response.End();
            //HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }
}