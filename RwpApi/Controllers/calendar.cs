using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Script.Serialization;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json;
using RwpApi.Models;

namespace RwpApi.Controllers
{
    [Authorize]
    public class CalendarController : ApiController
    {
        [HttpPost]
        [Route("api/calendar/PostCalendarLink")]
        public IHttpActionResult PostCalendarLink([FromBody] CalendarLink linkItem)
        {
            RSR rsr = CalendarLinks.CalendarLinkItem.AddCalendarLinkItem(linkItem);

            return Ok(rsr);
        }

        [HttpGet]
        [Route("api/calendar/RevokeCalendarLink/{guidLink}")]
        public IHttpActionResult RevokeCalendarLink(Guid guidLink)
        {
            RSR rsr = CalendarLinks.CalendarLinkItem.RevokeCalendarLink(guidLink);

            return Ok(rsr);
        }
    }

    public class OpenCalendarController : ApiController
    {
        [Route("api/opencalendar/GetCalendarForTeam/{linkID}")]
        public IHttpActionResult GetCalendarForTeam(string linkID)
        {
            RSR_CalItems items = RwpSlots.GetCalendarItemsForTeam(linkID);
            return Ok(items);
        }
    }
}
