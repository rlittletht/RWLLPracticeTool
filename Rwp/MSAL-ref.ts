
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

    configureForLogout() {
        $("#" + this.m_appClientConfig.idSignInButton)
            .html("Sign Out")
            .click((e) => {
                e.preventDefault();
                this.m_msal.logout();
                console.log("sign out requested");
            });
    }

    showWelcomeMessage()
    {
        var divWelcome = document.getElementById(this.m_sIdWelcome);

        divWelcome.innerHTML += 'Welcome ' + this.m_msal.MSAL().getUser().name;
        document.getElementById("SignIn").onclick = () => {
            console.log("in SignIn.onclick()");
            this.m_msal.MSAL().loginRedirect(this.appConfig.graphScopes);
        };

        this.configureForLogout();

        //        var loginbutton = document.getElementById('SignIn');
        //loginbutton.innerHTML = 'Sign Out';
        //loginbutton.setAttribute('onclick', 'signOut();');
    }

    onSignInSuccess()
    {
        this.showWelcomeMessage();
    }
}