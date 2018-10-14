
// A reference implementation of the MSAL interface for authentication and graph interaction
//
// This allows a SPA to get up and running very fast with MSAL, but with little to no actual
// customization in the experience. It just validates that LogIn/LogOut, and graph calls
// are working (which means the live auth stack and registration is correctly hooked up)
//
// To use, include MSAL-support.ts and MSAL-ref.ts, then provide the reference implementation
// to MSAL-support as the interface implemenation.

// this assumes that MSAL-support has already been included, as well as jquery

interface RefClientConfig
{
    idSignInButton: string;
}

class TCore_MSAL_RefImpl implements IMsalAppClient
{
    // expose through IMsalAppClient
    appConfig: AppConfig;

    // Internal members
    m_appClientConfig: RefClientConfig;
    m_sIdWelcome: string;

    m_msal: TCore_MSAL;

    constructor(appConfig: AppConfig, appClientConfig: RefClientConfig)
    {
        this.m_sIdWelcome = "WelcomeMessage";
        this.appConfig = appConfig;
        this.m_appClientConfig = appClientConfig;
    }

    bindMsal(msal: TCore_MSAL)
    {
        this.m_msal = msal;
    }

    configureForLogout()
    {
        $("#" + this.m_appClientConfig.idSignInButton)
            .attr("src", "signout.png")
            .unbind("click")
            .click((e) =>
            {
                e.preventDefault();
                this.m_msal.logout();
                console.log("sign out requested");
            });
    }

    configureForLogin()
    {
        $("#" + this.m_appClientConfig.idSignInButton)
            .attr("src", "signin.png")
            .unbind("click")
            .click((e) =>
            {
                e.preventDefault();
                this.m_msal.signInRedirect();
                console.log("sign inrequested");
            });
    }

    showWelcomeMessage()
    {
        var divWelcome = document.getElementById(this.m_sIdWelcome);

        divWelcome.innerHTML += 'Welcome ' + this.m_msal.MSAL().getUser().name;
        this.configureForLogout();
    }

    // for the reference implementation, just dump whatever we got back into the welcome message
    // (in real implementations, this is where you would deal with the results of the graph call)

    // also, there's no need for there to be a single callback function -- whenever you call
    // the graph, you can supply a new one...
    // (UNLESS you are using redirect???)
    graphAPICallback(data: any)
    {
        //Display user data on DOM
        var divWelcome = document.getElementById('WelcomeMessage');
        divWelcome.innerHTML += " to Microsoft Graph API!!";
        document.getElementById("json").innerHTML = JSON.stringify(data, null, 2);
    }

    onSignInSuccess()
    {
        this.showWelcomeMessage();
        this.m_msal.acquireTokenAndCallGraphAPI(this.appConfig.graphEndpoint, this.graphAPICallback, false);
    }
}