using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using RwpApi.Models;

namespace RwpApi.Controllers
{
    public class TeamController : ApiController
    {
        public HttpResponseMessage GetTestResult(string id)
        {
            Stream stm = new MemoryStream(4096);

            Teams teams = new Teams();

            teams.GetCsv(stm);
            stm.Flush();
            stm.Seek(0, SeekOrigin.Begin);

            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new StreamContent(stm);
            result.Content.Headers.ContentType = 
                new MediaTypeHeaderValue("application/octet-stream");
            return result;
        }
    }
}
