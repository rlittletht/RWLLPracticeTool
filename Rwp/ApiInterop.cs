﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Contexts;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Identity.Client;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using TCore;

namespace Rwp
{
    public partial class ApiInterop
    {
        private HttpContext m_context;
        private HttpClient m_client;
        private string m_sClientAccessToken; // used to determine if we can reuse a client

        private HttpServerUtility m_server;
        private string m_apiRoot;

        public ApiInterop(HttpContext context, HttpServerUtility server, string apiRoot)
        {
            m_context = context;
            m_server = server;
            m_apiRoot = apiRoot;
        }

        /*----------------------------------------------------------------------------
        	%%Function: ProcessResponse
        	%%Qualified: Rwp.ApiInterop.ProcessResponse
        	%%Contact: rlittle
        	
            General response code for all HttpResponses (for now, this is just
            looking for our special need-consent handshake)
        ----------------------------------------------------------------------------*/
        HttpResponseMessage ProcessResponse(HttpResponseMessage msg)
        {
            if (msg.StatusCode == HttpStatusCode.Unauthorized)
            {
                // check to see if we just need to get consent
                foreach (AuthenticationHeaderValue val in msg.Headers.WwwAuthenticate)
                {
                    if (val.Scheme == "need-consent")
                    {
                        // the parameter is the URL the user needs to visit in order to grant consent. Construct
                        // a link to report to the user (here we can inject HTML into our DIV to make
                        // this easier).

                        // This is not the best user experience (they end up in a new tab to grant consent, 
                        // and that tab is orphaned... but for now it makes it clear how a flow *could*
                        // work)
                        throw new Exception(
                            $"The current user has not given the WebApi consent to access the Microsoft Graph on their behalf. <a href='{val.Parameter}' target='_blank'>Click Here</a> to grant consent.");
                    }
                }
            }

            if (msg.StatusCode == HttpStatusCode.InternalServerError)
                throw new Exception(msg.ReasonPhrase);

            return msg;
        }
        /*----------------------------------------------------------------------------
        	%%Function: GetServiceResponse
        	%%Qualified: WebApp._default.GetServiceResponse

            call the webapi and get the response. broke this out because its nice
            to be able to collect all the async calls in the same place.

            would be really nice to use await in here, but every time i try it, i get
            threading issues, so old school task it is. 
        ----------------------------------------------------------------------------*/
        HttpResponseMessage GetServiceResponse(HttpClient client, string sTarget)
        {
            Task<HttpResponseMessage> tskResponse = client.GetAsync(sTarget);

            tskResponse.Wait();
            return ProcessResponse(tskResponse.Result);
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetServicePostResponse
        	%%Qualified: Rwp.ApiInterop.GetServicePostResponse
        	%%Contact: rlittle
        	
            Do an http post and get the response
        ----------------------------------------------------------------------------*/
        HttpResponseMessage GetServicePostResponse(HttpClient client, string sTarget, HttpContent content)
        {
            Task<HttpResponseMessage> tskResponse = client.PostAsync(sTarget, content);
            
            tskResponse.Wait();
            return ProcessResponse(tskResponse.Result);
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetServicePutResponse
        	%%Qualified: Rwp.ApiInterop.GetServicePutResponse
        	%%Contact: rlittle
        	
            Do an http put and get the response
        ----------------------------------------------------------------------------*/
        HttpResponseMessage GetServicePutResponse(HttpClient client, string sTarget, HttpContent content)
        {
            Task<HttpResponseMessage> tskResponse = client.PutAsync(sTarget, content);

            tskResponse.Wait();
            return ProcessResponse(tskResponse.Result);
        }


        bool FNeAccessToken(string s1, string s2)
        {
            if (s1 == null && s2 == null)
                return false;

            if (s1 == null || s2 == null)
                return true;

            return String.Compare(s1, s2, StringComparison.Ordinal) != 0;
        }
        /*----------------------------------------------------------------------------
        	%%Function: HttpClientCreate
        	%%Qualified: WebApp._default.HttpClientCreate
            
            setup the http client for the webapi calls we're going to make
        ----------------------------------------------------------------------------*/
        HttpClient HttpClientCreate(string sAccessToken)
        {
            if (m_client == null || FNeAccessToken(sAccessToken, m_sClientAccessToken))
            {
                HttpClient client = new HttpClient();

                // we have setup our webapi to take Bearer authentication, so add our access token
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sAccessToken);

                if (m_client != null)
                    m_client.Dispose();

                m_client = client;
                m_sClientAccessToken = sAccessToken;
            }

            return m_client;
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetUserId
        	%%Qualified: WebApp._default.GetUserId
        	
            convenient way to get the current user id (so we can get to the right
            TokenCache)
        ----------------------------------------------------------------------------*/
        string GetUserId()
        {
            if (ClaimsPrincipal.Current == null)
                return null;

            return ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetContextBase
        	%%Qualified: WebApp._default.GetContextBase
        	
            get the HttpContextBase we can use for the SessionState (which is needed
            by our TokenCache implemented by MSALSessionCache
        ----------------------------------------------------------------------------*/
        HttpContextBase GetContextBase()
        {
            return m_context.GetOwinContext().Environment["System.Web.HttpContextBase"] as HttpContextBase;
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetAccessToken
        	%%Qualified: WebApp._default.GetAccessToken

            Get an access token for accessing the WebApi. This will use 
            AcquireTokenSilentAsync to get the token. Since this is using the 
            same tokencache as we populated when the user logged in, we will
            get the access token from that cache. 
        ----------------------------------------------------------------------------*/
        string GetAccessToken()
        {
            // Retrieve the token with the specified scopes
            var scopes = new string[] {Startup.scopeWebApi};
            string userId = GetUserId();
            TokenCache tokenCache = new MSALSessionCache(userId, GetContextBase()).GetMsalCacheInstance();
            ConfidentialClientApplication cca = new ConfidentialClientApplication(Startup.clientId, Startup.authority,
                Startup.redirectUri, new ClientCredential(Startup.appKey), tokenCache, null);

            Task<IEnumerable<IAccount>> tskAccounts = cca.GetAccountsAsync();
            tskAccounts.Wait();

            IAccount account = tskAccounts.Result.FirstOrDefault();
            if (account == null)
                return null;

            Task<AuthenticationResult> tskResult =
                cca.AcquireTokenSilentAsync(scopes, account, Startup.authority, false);

            tskResult.Wait();
            return tskResult.Result.AccessToken;
        }

        /*----------------------------------------------------------------------------
        	%%Function: FTokenCachePopulated
        	%%Qualified: WebApp._default.FTokenCachePopulated
        	
        	return true if our TokenCache has been populated for the current 
            UserId.  Since our TokenCache is currently only stored in the session, 
            if our session ever gets reset, we might get into a state where there
            is a cookie for auth (and will let us automatically login), but the
            TokenCache never got populated (since it is only populated during the
            actual authentication process). If this is the case, we need to treat
            this as if the user weren't logged in. The user will SignIn again, 
            populating the TokenCache.
        ----------------------------------------------------------------------------*/
        public bool FTokenCachePopulated()
        {
            return MSALSessionCache.CacheExists(GetUserId(), GetContextBase());
        }

        #region Helper Service Calls
        /*----------------------------------------------------------------------------
        	%%Function: CallService
        	%%Qualified: Rwp.ApiInterop.CallService
        	%%Contact: rlittle
        	
            Core call service, returning an httpresponse
        ----------------------------------------------------------------------------*/
        public HttpResponseMessage CallService(string sTarget, bool fRequireAuth)
        {
            string sAccessToken = fRequireAuth ? GetAccessToken() : null;
            if (sAccessToken == null && fRequireAuth == true)
                throw new Exception("Authentication failed, no access token");

            HttpClient client = HttpClientCreate(sAccessToken);

            return GetServiceResponse(client, $"{m_apiRoot}/{sTarget}");
        }

        /*----------------------------------------------------------------------------
        	%%Function: CallService
        	%%Qualified: Rwp.ApiInterop.CallService<T>

            Call the service and parse the return value into the given type T        	
        ----------------------------------------------------------------------------*/
        public T CallService<T>(string sTarget, bool fRequireAuth)
        {
            HttpResponseMessage resp = CallService(sTarget, fRequireAuth);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new Exception("Service returned 'user is unauthorized'");
            }

            if (resp.StatusCode != HttpStatusCode.OK)
                throw new Exception(resp.ReasonPhrase);

            string sJson = GetContentAsString(resp);

            JavaScriptSerializer jsc = new JavaScriptSerializer();

            return jsc.Deserialize<T>(sJson);
        }

        /*----------------------------------------------------------------------------
        	%%Function: CallServicePut
        	%%Qualified: Rwp.ApiInterop.CallServicePut

            Call the service with a put, with the given HttpContent        	
        ----------------------------------------------------------------------------*/
        public HttpResponseMessage CallServicePut(string sTarget, HttpContent content, bool fRequireAuth)
        {
            string sAccessToken = GetAccessToken();
            if (sAccessToken == null && fRequireAuth == true)
                throw new Exception("Authentication failed, no access token");

            HttpClient client = HttpClientCreate(sAccessToken);

            return GetServicePutResponse(client, $"{m_apiRoot}/{sTarget}", content);
        }

        /*----------------------------------------------------------------------------
        	%%Function: CallServicePut
        	%%Qualified: Rwp.ApiInterop.CallServicePut<T>

            Call the service put, and parse the result into the given type T
        ----------------------------------------------------------------------------*/
        public T CallServicePut<T>(string sTarget, HttpContent content, bool fRequireAuth)
        {
            HttpResponseMessage resp = CallServicePut(sTarget, content, fRequireAuth);

            string sJson = GetContentAsString(resp);

            JavaScriptSerializer jsc = new JavaScriptSerializer();

            return jsc.Deserialize<T>(sJson);
        }

        /*----------------------------------------------------------------------------
        	%%Function: CallServicePost
        	%%Qualified: Rwp.ApiInterop.CallServicePost
        	%%Contact: rlittle
        	
            Core call put service, returning an httpresponse
        ----------------------------------------------------------------------------*/
        public HttpResponseMessage CallServicePost(string sTarget, HttpContent content, bool fRequireAuth)
        {
            string sAccessToken = GetAccessToken();
            if (sAccessToken == null && fRequireAuth == true)
                throw new Exception("Authentication failed, no access token");

            HttpClient client = HttpClientCreate(sAccessToken);

            return GetServicePostResponse(client, $"{m_apiRoot}/{sTarget}", content);
        }

        /*----------------------------------------------------------------------------
        	%%Function: CallServicePost
        	%%Qualified: Rwp.ApiInterop.CallServicePost<T>

            Call the service put, and parse the result into the given type T
        ----------------------------------------------------------------------------*/
        public T1 CallServicePost<T1, T2>(string sTarget, T2 t2, bool fRequireAuth)
        {
            JavaScriptSerializer jsc = new JavaScriptSerializer();

            string s = jsc.Serialize(t2);

            s = JsonConvert.SerializeObject(t2);

            HttpContent content = new StringContent(s, Encoding.UTF8, "application/json");
            HttpResponseMessage resp = CallServicePost(sTarget, content, fRequireAuth);

            string sJson = GetContentAsString(resp);
            
            return jsc.Deserialize<T1>(sJson);
        }
        #endregion

        /*----------------------------------------------------------------------------
        	%%Function: GetContentAsString
        	%%Qualified: Rwp.ApiInterop.GetContentAsString

            convert the HttpResponseMessage into a string
        ----------------------------------------------------------------------------*/
        public string GetContentAsString(HttpResponseMessage resp)
        {
            Task<string> tskString = resp.Content.ReadAsStringAsync();

            tskString.Wait();
            return tskString.Result;
        }


    }
}