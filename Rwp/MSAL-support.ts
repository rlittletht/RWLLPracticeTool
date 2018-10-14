declare var Msal: any;

// this is the configuration of this MSAL Application
interface AppConfig
{
    clientID: string;
    graphScopes: string[];
    graphEndpoint: string;
}

// Client callbacks for this client application. This is where you
// get to customize your experience
interface IMsalAppClient
{
    appConfig: AppConfig;

    bindMsal(msal: TCore_MSAL);
    onSignInSuccess();
    configureForLogout();
    configureForLogin();
}

class TCore_MSAL
{
    msalUserAgent: any;
    m_appClient: IMsalAppClient;

    /*----------------------------------------------------------------------------
    	%%Function: constructor
    	%%Contact: rlittle
    ----------------------------------------------------------------------------*/
    constructor(appClient: IMsalAppClient)
    {
        this.m_appClient = appClient;
        this.msalUserAgent = new Msal.UserAgentApplication(appClient.appConfig.clientID,
            null,
            this.acquireTokenRedirectCallBack,
            { storeAuthStateInCookie: true, cacheLocation: "sessionStorage" });
    }

    MSAL(): any
    {
        return this.msalUserAgent;
    }

    /*----------------------------------------------------------------------------
	    %%Function: signIn
	    %%Contact: rlittle

        prompts the users for signin	    
    ----------------------------------------------------------------------------*/
    signInPopup()
    {
        this.msalUserAgent.loginPopup(this.m_appClient.appConfig.graphScopes)
            .then((idToken) =>
            {
                //Login Success
                this.m_appClient.onSignInSuccess();
            },
            (error) =>
            {
                console.log(error);
            });
    }

    signInRedirect()
    {
        console.log("in SignIn.onclick()");
        this.msalUserAgent.loginRedirect(this.m_appClient.appConfig.graphScopes);
    }

    /*----------------------------------------------------------------------------
	    %%Function: callGraphAPI
	    %%Contact: rlittle

        make a call to the graph. invokes callback when the trasnfer is complete.
    ----------------------------------------------------------------------------*/
    callGraphAPI(theUrl: string, accessToken: string, callback: any)
    {
        let xmlHttp: any = new XMLHttpRequest();
        xmlHttp.onreadystatechange = function()
        {
            if (this.readyState == 4 && this.status == 200)
                callback(JSON.parse(this.responseText));
        }
        xmlHttp.open("GET", theUrl, true); // true for asynchronous
        xmlHttp.setRequestHeader('Authorization', 'Bearer ' + accessToken);
        xmlHttp.send();
    }

    /*----------------------------------------------------------------------------
	    %%Function: acquireTokenAndCallGraphAPI
	    %%Contact: rlittle

        when you want to call a graph API, you need to have an access token -- 
        this gets one for you and calls the graph. 

        if you have several API's to call, you should acquire the token 
        separately and hold on to it.
    ----------------------------------------------------------------------------*/
    acquireTokenAndCallGraphAPI(sEndpoint: string, callback: (arg: any) =>any, fRedirect: boolean)
    {
        this.msalUserAgent.acquireTokenSilent(this.m_appClient.appConfig.graphScopes)
            .then(
                (accessToken) =>
                {
                    this.callGraphAPI(sEndpoint, accessToken, callback);
                },
                (error) =>
                {
                    console.log(error);
                    // Call in case of acquireTokenSilent failure due to consent or interaction required ONLY
                    if (error.indexOf("consent_required") !== -1
                        || error.indexOf("interaction_required") !== -1
                        || error.indexOf("login_required") !== -1)
                    {
                        if (!fRedirect)
                        {
                            this.msalUserAgent.acquireTokenPopup(this.m_appClient.appConfig.graphScopes)
                                .then(
                                    (accessToken) => { this.callGraphAPI(sEndpoint, accessToken, callback); },
                                    (error) => { console.log(error); }
                                );
                        }
                        else // we need to use redirect instead of popup (just a UI choice...or accommodating IE)
                        {
                            //Call acquireTokenRedirect in case of acquireToken Failure
                            if (error.indexOf("consent_required") !== -1
                                || error.indexOf("interaction_required") !== -1
                                || error.indexOf("login_required") !== -1)
                            {
                                // HOW does this call the graph API??
                                this.msalUserAgent.acquireTokenRedirect(this.m_appClient.appConfig.graphScopes);
                            }
                        }
                    }
                });
    }

    /*----------------------------------------------------------------------------
	    %%Function: logout
	    %%Contact: rlittle
	    
        logout. will prompt for another login.
    ----------------------------------------------------------------------------*/
    logout()
    {
        this.msalUserAgent.logout();
    }

    // This function can be removed if you do not need to support IE
    acquireTokenRedirectCallBack(errorDesc: string, token: string, error: any, tokenType: string)
    {
        debugger;
        
        if (tokenType === "access_token")
        {
            alert("NYI: what to do here?");
            // this.callGraphAPI(this.m_appClient.appConfig.graphEndpoint, token, this.graphAPICallback);
        }
        else
        {
            console.log("token type is:" + tokenType);
        }
    }

    static initialize(document, appClient: IMsalAppClient): TCore_MSAL
    {
        // Browser check variables
        let ua: string = window.navigator.userAgent;
        let msie: number = ua.indexOf('MSIE ');
        let msie11: number = ua.indexOf('Trident/');
        let msedge: number = ua.indexOf('Edge/');
        let isIE: boolean = msie > 0 || msie11 > 0;
        let isEdge: boolean = msedge > 0;

        let msal: TCore_MSAL = new TCore_MSAL(appClient);
        appClient.bindMsal(msal);

        console.log("in initialize");
            $(document)
                .ready(() =>
                {
                    console.log("in document.ready()");
                    appClient.configureForLogin();

                    if (msal.MSAL().getUser() && !msal.MSAL().isCallback(window.location.hash))
                    { // avoid duplicate code execution on page load in case of iframe and popup window.
                        msal.m_appClient.onSignInSuccess();
                    }
                }); 
        return msal;
    }
}
