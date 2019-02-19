using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Security.OAuth;
using RwpApi.Models;

namespace RwpApi.Controllers
{
    //[Authorize]
    public class CoreController : ApiController
    {
        static string ExtractServerNameFromConnection(string sConnection)
        {
            Regex rex = new Regex(".*server[ \t]*=[ \t]*([^;]*)[ \t]*;.*", RegexOptions.IgnoreCase);

            Match m = rex.Match(sConnection);
       
            return m.Groups[1].Value;
        }

        [Route("api/core/GetServerInfo")]
        public IHttpActionResult GetServerInfo()
        {
            ServerInfo si = new ServerInfo();

            si.sServerName = System.Net.Dns.GetHostName();

            si.sSqlServerHash = ExtractServerNameFromConnection(Startup._sResourceConnString);
            return Ok(si);
        }
    }
}
