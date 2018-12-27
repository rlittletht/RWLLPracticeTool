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
using Newtonsoft.Json;
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

        [Route("api/slot/GetCalendarForTeam/{team}")]
        public IHttpActionResult GetCalendarForTeam(string team)
        {
            RSR_CalItems items = RwpSlots.GetCalendarItemsForTeam(team);
            return Ok(items);
        }

        [HttpGet]
        [Route("api/slot/DeleteSlotsByYear/{year}")]
        public IHttpActionResult DeleteSlotsByYear(int year)
        {
            RSR sr;

            sr = RwpSlots.ClearYear(year);
            return Ok(sr);
        }

        [HttpGet]
        [Route("api/slot/DeleteAllSlots")]
        public IHttpActionResult DeleteAllSlots()
        {
            RSR sr;

            sr = RwpSlots.ClearAll();
            return Ok(sr);
        }

        [HttpPut]
        [Route("api/slot/PutSlots")]
        public IHttpActionResult PutSlots(HttpRequestMessage request)
        {
            Task<Stream> stm = request.Content.ReadAsStreamAsync();

            stm.Wait();

            RSR sr;

            sr = RwpSlots.ImportCsv(stm.Result);
            return Ok(sr);
        }
    }
}
