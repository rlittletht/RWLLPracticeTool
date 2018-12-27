using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Security.OAuth;
using RwpApi.Models;

namespace RwpApi.Controllers
{
//    [Authorize]
    public class SlotController : ApiController
    {
        [Route("api/slot/GetSlots")]
        public HttpResponseMessage GetSlots()
        {
            Stream stm = new MemoryStream(4096);

            RwpSlots slots = new RwpSlots();

            slots.GetCsv(stm);
            stm.Flush();
            stm.Seek(0, SeekOrigin.Begin);

            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new StreamContent(stm);
            result.Content.Headers.ContentType = 
                new MediaTypeHeaderValue("application/octet-stream");
            return result;
        }

        [HttpGet]
        [Route("api/slot/DeleteSlotsByYear/{year}")]
        public IHttpActionResult DeleteSlotsByYear(int year)
        {
            RSR sr;

            sr = RwpSlots.ClearYear(year);
            return Ok(sr);
        }

    }
}
