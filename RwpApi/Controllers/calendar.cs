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
    public class CalendarController : ApiController
    {
        [Route("api/calendar/GetCalendarForTeam/{team}")]
        public IHttpActionResult GetCalendarForTeam(string team)
        {
            RSR_CalItems items = RwpSlots.GetCalendarItemsForTeam(team);
            return Ok(items);
        }

        [Route("api/calendar/PutCalendarLink/{linkItem}")]
        public IHttpActionResult PutCalendarLink(CalendarLink linkItem)
        {
            RSR rsr = CalendarLinks.CalendarLinkItem.AddCalendarLinkItem(linkItem);
            return Ok(rsr);
        }
    }
}
