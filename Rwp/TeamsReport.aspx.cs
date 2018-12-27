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
	public partial class TeamsReport : System.Web.UI.Page
	{
	    private ApiInterop m_apiInterop;

		protected void Page_Load(object sender, EventArgs e)
		{
		    m_apiInterop = new ApiInterop(Context, Server);

    		DoReport();
		}

	    protected void DoReport()
	    {
	        HttpResponseMessage resp = m_apiInterop.CallService("http://localhost/rwpapi/api/team/GetTeams", false);

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

			Response.Write("\n");
			Response.Flush();
	        HttpContext.Current.ApplicationInstance.CompleteRequest();
		}
	}
}
