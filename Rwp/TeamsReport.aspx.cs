using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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

namespace Rwp
{
	public partial class TeamsReport : System.Web.UI.Page
	{
	    private ApiInterop m_apiInterop;

		protected void Page_Load(object sender, EventArgs e)
		{
		    m_apiInterop = new ApiInterop(Context, Server);

    		DoReport();
		}

	    private RwpSvcProxy.RSR CheckIP()
	    {
	        RwpSvcProxy.RSR sr = new RwpSvcProxy.RSR();

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
	        HttpResponseMessage resp = m_apiInterop.CallService("http://localhost/rwpapi/api/team/GetTeams", false);

	        RwpSvcProxy.PracticeClient rspClient = new RwpSvcProxy.PracticeClient("BasicHttpBinding_PracticeStream");

	        Task<Stream> tskStream = resp.Content.ReadAsStreamAsync();
	        tskStream.Wait();

	        Stream stm = tskStream.Result;
    	    TextReader tr = new StreamReader(stm);

			Response.Clear();
			Response.ContentType = "text/csv";
			Response.AddHeader("Content-Disposition", "filename=\"Teams.csv\"");

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
