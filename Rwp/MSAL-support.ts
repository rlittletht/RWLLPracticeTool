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
            { storeAuthStateInCookie: true, cacheLocation: "localStorage" });
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
    signIn()
    {
        this.msalUserAgent.loginPopup(this.m_appClient.appConfig.graphScopes)
            .then((idToken) =>
            {
                //Login Success
                this.m_appClient.onSignInSuccess();
                // this probably is only a ref impl thing...don't always want to call the graph on successful login
                this.acquireTokenPopupAndCallMSGraph(); 
            },
            (error) =>
            {
                console.log(error);
            });
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

    acquireTokenPopupAndCallMSGraph()
    {
        this.acquireTokenAndCallMSGraph(this.m_appClient.appConfig.graphEndpoint, this.graphAPICallback, false/*fRedirect*/);
    }

    acquireTokenPopupAndCallMSGraphOrig()
    {
        //Call acquireTokenSilent (iframe) to obtain a token for Microsoft Graph
        this.msalUserAgent.acquireTokenSilent(this.m_appClient.appConfig.graphScopes)
            .then((accessToken) =>
                {
                this.callGraphAPI(this.m_appClient.appConfig.graphEndpoint, accessToken, this.graphAPICallback);
                },
                (error) =>
                {
                    console.log(error);
                    // Call acquireTokenPopup (popup window) in case of acquireTokenSilent failure due to consent or interaction required ONLY
                    if (error.indexOf("consent_required") !== -1
                        || error.indexOf("interaction_required") !== -1
                        || error.indexOf("login_required") !== -1)
                    {
                        this.msalUserAgent.acquireTokenPopup(this.m_appClient.appConfig.graphScopes)
                            .then((accessToken) =>
                                {
                                    this.callGraphAPI(this.m_appClient.appConfig.graphEndpoint,
                                        accessToken,
                                        this.graphAPICallback);
                                },
                                (error) =>
                                {
                                    console.log(error);
                                });
                    }
                });
    }

    acquireTokenAndCallMSGraph(sEndpoint: string, callback: any, fRedirect: boolean)
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

    graphAPICallback(data: any)
    {
        //Display user data on DOM
        var divWelcome = document.getElementById('WelcomeMessage');
        divWelcome.innerHTML += " to Microsoft Graph API!!";
        document.getElementById("json").innerHTML = JSON.stringify(data, null, 2);
    }

    logout()
    {
        this.msalUserAgent.logout();
    }

    //showWelcomeMessage()
    //{
        //var divWelcome = document.getElementById('WelcomeMessage');
        //divWelcome.innerHTML += 'Welcome ' + this.msalUserAgent.getUser().name;
        //document.getElementById("SignIn").onclick = () => {
            //console.log("in SignIn.onclick()");
            //this.msalUserAgent.loginRedirect(this.m_appClient.appConfig.graphScopes);
        //};
//
        //this.configureForLogout();
    //}

    // This function can be removed if you do not need to support IE
    acquireTokenRedirectAndCallMSGraph()
    {
        this.acquireTokenAndCallMSGraph(this.m_appClient.appConfig.graphEndpoint, this.graphAPICallback, true/*fRedirect*/);
    }

    acquireTokenRedirectAndCallMSGraphOrig()
    {
        //Call acquireTokenSilent (iframe) to obtain a token for Microsoft Graph
        this.msalUserAgent.acquireTokenSilent(this.m_appClient.appConfig.graphScopes)
            .then((accessToken) =>
            {
                this.callGraphAPI(this.m_appClient.appConfig.graphEndpoint, accessToken, this.graphAPICallback);
            },
            (error) =>
            {
                console.log(error);
                //Call acquireTokenRedirect in case of acquireToken Failure
                if (error.indexOf("consent_required") !== -1
                    || error.indexOf("interaction_required") !== -1
                    || error.indexOf("login_required") !== -1)
                {
                    this.msalUserAgent.acquireTokenRedirect(this.m_appClient.appConfig.graphScopes);
                }
            });
    }

    acquireTokenRedirectCallBack(errorDesc: string, token: string, error: any, tokenType: string)
    {
        if (tokenType === "access_token")
        {
            this.callGraphAPI(this.m_appClient.appConfig.graphEndpoint, token, this.graphAPICallback);
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
        //If you support IE, our recommendation is that you sign-in using Redirect APIs
        //If you as a developer are testing using Edge InPrivate mode, please add "isEdge" to the if check
        //if (!isIE) {
        //if (msal.MSAL().getUser()) { // avoid duplicate code execution on page load in case of iframe and popup window.
        //msal.showWelcomeMessage();
        //msal.acquireTokenPopupAndCallMSGraph();
        //}
        //}
        //else 
        {
            $(document)
                .ready(() =>
                {
                    console.log("in document.ready()");
                    document.getElementById("SignIn").onclick = function()
                    {
                        console.log("in SignIn.onclick()");
                        msal.MSAL().loginRedirect(appClient.appConfig.graphScopes);
                    };

                    if (msal.MSAL().getUser() && !msal.MSAL().isCallback(window.location.hash))
                    { // avoid duplicate code execution on page load in case of iframe and popup window.
                        msal.m_appClient.onSignInSuccess();
                        msal.acquireTokenRedirectAndCallMSGraph();
                    }
                });
        }

        return msal;
    }
}
