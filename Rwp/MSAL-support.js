var foo = /** @class */ (function () {
    function foo() {
        this.m_a = 1;
    }
    return foo;
}());
var TCore_MSAL = /** @class */ (function () {
    function TCore_MSAL(appConfig) {
        this.m_appConfig = appConfig;
        this.m_myMSALObj = new Msal.UserAgentApplication(appConfig.clientID, null, this.acquireTokenRedirectCallBack, { storeAuthStateInCookie: true, cacheLocation: "localStorage" });
    }
    TCore_MSAL.prototype.MSAL = function () {
        return this.m_myMSALObj;
    };
    TCore_MSAL.prototype.signIn = function () {
        var _this = this;
        this.m_myMSALObj.loginPopup(this.m_appConfig.graphScopes)
            .then(function (idToken) {
            //Login Success
            _this.showWelcomeMessage();
            _this.acquireTokenPopupAndCallMSGraph();
        }, function (error) {
            console.log(error);
        });
    };
    TCore_MSAL.prototype.callMSGraph = function (theUrl, accessToken, callback) {
        var xmlHttp = new XMLHttpRequest();
        xmlHttp.onreadystatechange = function () {
            if (this.readyState == 4 && this.status == 200)
                callback(JSON.parse(this.responseText));
        };
        xmlHttp.open("GET", theUrl, true); // true for asynchronous
        xmlHttp.setRequestHeader('Authorization', 'Bearer ' + accessToken);
        xmlHttp.send();
    };
    TCore_MSAL.prototype.acquireTokenPopupAndCallMSGraph = function () {
        var _this = this;
        //Call acquireTokenSilent (iframe) to obtain a token for Microsoft Graph
        this.m_myMSALObj.acquireTokenSilent(this.m_appConfig.graphScopes)
            .then(function (accessToken) {
            _this.callMSGraph(_this.m_appConfig.graphEndpoint, accessToken, _this.graphAPICallback);
        }, function (error) {
            console.log(error);
            // Call acquireTokenPopup (popup window) in case of acquireTokenSilent failure due to consent or interaction required ONLY
            if (error.indexOf("consent_required") !== -1
                || error.indexOf("interaction_required") !== -1
                || error.indexOf("login_required") !== -1) {
                _this.m_myMSALObj.acquireTokenPopup(_this.m_appConfig.graphScopes)
                    .then(function (accessToken) {
                    _this.callMSGraph(_this.m_appConfig.graphEndpoint, accessToken, _this.graphAPICallback);
                }, function (error) {
                    console.log(error);
                });
            }
        });
    };
    TCore_MSAL.prototype.graphAPICallback = function (data) {
        //Display user data on DOM
        var divWelcome = document.getElementById('WelcomeMessage');
        divWelcome.innerHTML += " to Microsoft Graph API!!";
        document.getElementById("json").innerHTML = JSON.stringify(data, null, 2);
    };
    TCore_MSAL.prototype.showWelcomeMessage = function () {
        var divWelcome = document.getElementById('WelcomeMessage');
        divWelcome.innerHTML += 'Welcome ' + this.m_myMSALObj.getUser().name;
        var loginbutton = document.getElementById('SignIn');
        loginbutton.innerHTML = 'Sign Out';
        loginbutton.setAttribute('onclick', 'signOut();');
    };
    // This function can be removed if you do not need to support IE
    TCore_MSAL.prototype.acquireTokenRedirectAndCallMSGraph = function () {
        var _this = this;
        //Call acquireTokenSilent (iframe) to obtain a token for Microsoft Graph
        this.m_myMSALObj.acquireTokenSilent(this.m_appConfig.graphScopes)
            .then(function (accessToken) {
            _this.callMSGraph(_this.m_appConfig.graphEndpoint, accessToken, _this.graphAPICallback);
        }, function (error) {
            console.log(error);
            //Call acquireTokenRedirect in case of acquireToken Failure
            if (error.indexOf("consent_required") !== -1
                || error.indexOf("interaction_required") !== -1
                || error.indexOf("login_required") !== -1) {
                _this.m_myMSALObj.acquireTokenRedirect(_this.m_appConfig.graphScopes);
            }
        });
    };
    TCore_MSAL.prototype.acquireTokenRedirectCallBack = function (errorDesc, token, error, tokenType) {
        if (tokenType === "access_token") {
            this.callMSGraph(this.m_appConfig.graphEndpoint, token, this.graphAPICallback);
        }
        else {
            console.log("token type is:" + tokenType);
        }
    };
    return TCore_MSAL;
}());
;
(function (d) {
    // Browser check variables
    var ua = window.navigator.userAgent;
    var msie = ua.indexOf('MSIE ');
    var msie11 = ua.indexOf('Trident/');
    var msedge = ua.indexOf('Edge/');
    var isIE = msie > 0 || msie11 > 0;
    var isEdge = msedge > 0;
    var appConfig = {
        clientID: "1234",
        graphEndpoint: "foo",
        graphScopes: [""]
    };
    var msal = new TCore_MSAL(appConfig);
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
        $(d)
            .ready(function () {
            d.getElementById("SignIn").onclick = function () {
                msal.MSAL().loginRedirect(appConfig.graphScopes);
            };
            if (msal.MSAL().getUser() && !msal.MSAL().isCallback(window.location.hash)) { // avoid duplicate code execution on page load in case of iframe and popup window.
                msal.showWelcomeMessage();
                msal.acquireTokenRedirectAndCallMSGraph();
            }
        });
    }
})(document);
//# sourceMappingURL=MSAL-support.js.map