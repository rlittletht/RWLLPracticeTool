using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OAuth;
using RwpApi.Models;

namespace RwpApi.Controllers
{
    [Authorize]
    public class TeamController : ApiController
    {
        [Route("api/team/GetTeams")]
        public HttpResponseMessage GetTeams()
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

        [HttpPut]
        [Route("api/team/PutTeams")]
        public IHttpActionResult PutTeams(HttpRequestMessage request)
        {
            Task<Stream> stm = request.Content.ReadAsStreamAsync();

            stm.Wait();

            RSR sr;

            sr = Teams.ImportCsv(stm.Result);
            return Ok(sr);
        }

        [HttpGet]
        [Route("api/team/DeleteTeams")]
        public IHttpActionResult DeleteTeams()
        {
            RSR sr;

            sr = Teams.ClearTeams();
            return Ok(sr);
        }

        [HttpGet]
        [Route("api/team/AddTeamUser")]
        public IHttpActionResult AddTeamUser([FromUri] string Identity, [FromUri] string Tenant, [FromUri] string TeamName, [FromUri] string Division, [FromUri] string Email, [FromUri] bool AddTeam)
        {
            RSR sr;

            if (String.IsNullOrEmpty(Email))
                Email = Identity;

            sr = Teams.AddTeamUser(Identity, Tenant, TeamName, Division, Email, AddTeam);
            return Ok(sr);
        }

        [HttpGet]
        [Route("api/team/CreateUpdateTeam")]
        public IHttpActionResult CreateUpdateTeam(
            [FromUri] string TeamName, 
            [FromUri] string Division, 
            [FromUri] bool GenerateInvitationCode,
            [FromUri] bool ShowInvitationCode,
            [FromUri] bool UpdateInformation)
        {
            RSR sr;

            sr = Teams.CreateUpdateTeam(TeamName, Division, GenerateInvitationCode, ShowInvitationCode, UpdateInformation);
            return Ok(sr);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("api/team/RedeemInvitation")]
        public IHttpActionResult RedeemInvitation([FromUri] string Identity, [FromUri] string Tenant, [FromUri] string InvitationCode)
        {
            RSR sr = Teams.RedeemInvitation(Identity, Tenant, InvitationCode);

            return Ok(sr);
        }

        [HttpGet]
        [Route("api/team/DeleteAuthentications")]
        public IHttpActionResult DeleteAuthentications()
        {
            RSR sr;

            sr = Teams.ClearAuth();
            return Ok(sr);
        }

    }
}
