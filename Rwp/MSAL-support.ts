﻿declare var Msal: any;

interface IMsalAppClient
{
    clientID: string;
    graphScopes: string[];
    graphEndpoint: string;
    idSignInButton: string;
}

class TCore_MSAL
{
    m_myMSALObj: any;
    m_appConfig: IMsalAppClient;

    constructor(appConfig: IMsalAppClient)
    {
        this.m_appConfig = appConfig;
        this.m_myMSALObj = new Msal.UserAgentApplication(appConfig.clientID,
            null,
            this.acquireTokenRedirectCallBack,
            { storeAuthStateInCookie: true, cacheLocation: "localStorage" });
    }

    MSAL(): any
    {
        return this.m_myMSALObj;
    }

    signIn()
    {
        this.m_myMSALObj.loginPopup(this.m_appConfig.graphScopes)
            .then((idToken) =>
            {
                //Login Success
                this.showWelcomeMessage();
                this.acquireTokenPopupAndCallMSGraph();
            },
            (error) =>
            {
                console.log(error);
            });
    }

    callMSGraph(theUrl: string, accessToken: string, callback: any)
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
        //Call acquireTokenSilent (iframe) to obtain a token for Microsoft Graph
        this.m_myMSALObj.acquireTokenSilent(this.m_appConfig.graphScopes)
            .then((accessToken) =>
            {
                this.callMSGraph(this.m_appConfig.graphEndpoint, accessToken, this.graphAPICallback);
            },
            (error) =>
            {
                console.log(error);
                // Call acquireTokenPopup (popup window) in case of acquireTokenSilent failure due to consent or interaction required ONLY
                if (error.indexOf("consent_required") !== -1
                    || error.indexOf("interaction_required") !== -1
                    || error.indexOf("login_required") !== -1)
                {
                    this.m_myMSALObj.acquireTokenPopup(this.m_appConfig.graphScopes)
                        .then((accessToken) =>
                        {
                            this.callMSGraph(this.m_appConfig.graphEndpoint, accessToken, this.graphAPICallback);
                        },
                        (error) =>
                        {
                            console.log(error);
                        });
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

    configureForLogout()
    {
        $("#" + this.m_appConfig.idSignInButton)
            .html("Sign Out")
            .click((e) =>
            {
                e.preventDefault();
                this.m_myMSALObj.logout();
                console.log("sign out requested");
            });
    }

    showWelcomeMessage()
    {
        var divWelcome = document.getElementById('WelcomeMessage');
        divWelcome.innerHTML += 'Welcome ' + this.m_myMSALObj.getUser().name;
        document.getElementById("SignIn").onclick = () => {
            console.log("in SignIn.onclick()");
            this.m_myMSALObj.loginRedirect(this.m_appConfig.graphScopes);
        };

        this.configureForLogout();

//        var loginbutton = document.getElementById('SignIn');
        //loginbutton.innerHTML = 'Sign Out';
        //loginbutton.setAttribute('onclick', 'signOut();');
    }

    // This function can be removed if you do not need to support IE
    acquireTokenRedirectAndCallMSGraph()
    {
        //Call acquireTokenSilent (iframe) to obtain a token for Microsoft Graph
        this.m_myMSALObj.acquireTokenSilent(this.m_appConfig.graphScopes)
            .then((accessToken) =>
            {
                this.callMSGraph(this.m_appConfig.graphEndpoint, accessToken, this.graphAPICallback);
            },
            (error) =>
            {
                console.log(error);
                //Call acquireTokenRedirect in case of acquireToken Failure
                if (error.indexOf("consent_required") !== -1
                    || error.indexOf("interaction_required") !== -1
                    || error.indexOf("login_required") !== -1)
                {
                    this.m_myMSALObj.acquireTokenRedirect(this.m_appConfig.graphScopes);
                }
            });
    }

    acquireTokenRedirectCallBack(errorDesc: string, token: string, error: any, tokenType: string)
    {
        if (tokenType === "access_token")
        {
            this.callMSGraph(this.m_appConfig.graphEndpoint, token, this.graphAPICallback);
        }
        else
        {
            console.log("token type is:" + tokenType);
        }
    }

    static initialize(document, appConfig: IMsalAppClient): TCore_MSAL
    {
        // Browser check variables
        let ua: string = window.navigator.userAgent;
        let msie: number = ua.indexOf('MSIE ');
        let msie11: number = ua.indexOf('Trident/');
        let msedge: number = ua.indexOf('Edge/');
        let isIE: boolean = msie > 0 || msie11 > 0;
        let isEdge: boolean = msedge > 0;

        let msal: TCore_MSAL = new TCore_MSAL(appConfig);
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
                        msal.MSAL().loginRedirect(appConfig.graphScopes);
                    };

                    if (msal.MSAL().getUser() && !msal.MSAL().isCallback(window.location.hash))
                    { // avoid duplicate code execution on page load in case of iframe and popup window.
                        msal.showWelcomeMessage();
                        msal.acquireTokenRedirectAndCallMSGraph();
                    }
                });
        }

        return msal;
    }
}
