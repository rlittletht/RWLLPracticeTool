﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ReleaseNotes.aspx.cs" Inherits="Rwp.ReleaseNotes" %>

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
        <ul><li>2/27/2020@23:23</li><ul><li>Changed all backend dates to UTC</li><li>Changed reset time from midnight PST to noon PST</li></ul>
            <li>2/26/2017@13:45</li>
                <ul><li>Updated front page to reflect school slots available</li></ul>
            <li>2/25/2017</li>
                <ul><li>Pointed services to thetasoft2</li></ul>
            <li>2/19/2017@15:32</li>
                <ul><li>Implemented iCalendar feed</li></ul>
            <li>2/18/2017@12:30
                <ul><li>Moved to new host, practice slots released</li></ul>
            </li>
            <li>2/21/2016@13:11
                <ul>
                    <li>Added reserved date/time to the main grid</li>
                </ul>
            </li>
            <li>2/20/2016@20:04
                <ul>
                    <li>Practice slots for ALL 60' fields are loaded and available signup</li>
                </ul>
            </li>
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
