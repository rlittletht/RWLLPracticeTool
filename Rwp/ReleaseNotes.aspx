<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ReleaseNotes.aspx.cs" Inherits="Rwp.ReleaseNotes" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link rel="stylesheet" href="rwp.css"/>
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div class="masthead">release notes</div>
        <ul>
            <li>8/29/2015@19:02
                <ul>Internal only changes</ul>
            </li>
            <li>2/15/2015@19:00
                <ul>
                    <li>Practice slots through 3/20 are loaded
                    <li>Team logins created and distributed
                </ul>
            <li>2/18/2014@21:00
                <ul>
                    <li>Fixed reservation logic to correctly reflect enabled/disabled state (and report reasons)
                </ul>
            <li>1/18/2014@4:04pm
                <ul>
                    <li>Fixed hardcoded 2013 dates to use the current year instead of being hardcoded
                </ul>
            <li>2/26/2013@11:45pm
                <ul><li>Fixed server date logic to calculate PST from UTC accurately</li>
                    <li>Fixed release field logic to accurately reflect the fact that you cannot release fields on the same day as the field slot (this matches the TSQL logic in the usp_UpdateSlot stored procedure from previous years</li>
                </ul>
            </li>
            <li>2/21/2013@10:30am
                <ul><li>Fixed stored procedure logic to properly account for UTC date storage in database.  Days should now rollover at midnight</li>
                    <li>Patched data in database to reflect local PST instead of UTC</li>
                </ul>
            </li>
            <li>2/20/2013@8:31pm
                <ul><li>Updated field reservation rules for turf fields -- Coast and above = 3 turf touches; AAA and below = 2 turf touches</li></ul>
            </li>
            <li>2/19/2013@11:39pm
            <ul>
                <li>Updated field logic in stored procedures to treat H5/H6/H5a/H5b/H6a/H6b equivalent</li>
            </ul>
                </li>
            <li>2/19/2013@3:00am
                <ul><li>Thetasoft site online</li></ul>
            </li>
        </ul>
    </div>
    </form>
</body>
</html>
