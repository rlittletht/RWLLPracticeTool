<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="Rwp.default1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title></title>
    <link rel="stylesheet" href="rwp.css"/>
</head>
<body style="height: 244">
<form id="Form1" runat="server" style="text-align: center" enctype="multipart/form-data">
  <div class="mastHeadButtons">
    <asp:ImageButton ID="LoginOutButton" runat="server" ImageUrl="signin.png" CssClass="loginButton" />
  </div>
    <p style="display: table-cell; vertical-align: middle; height: 30pt; width: 24in; background: gold;">

        <font face="Times" size="4">
            <asp:Label ID="Message0" align="Center" Font-Bold="True" BackColor="gold" Width="100%"
                runat="server" />
        </font>
    </p>
    <font face="Verdana" size="3">
        <asp:Label ID="Message1" align="Center" runat="server" />
    </font>
     
<center>
    <table border="0">
        <tr>
            <td>
                <table border="1" cellpadding="3" cellspacing="0" bordercolor="black" bgcolor="#acacac">
                    <tr>
	                    <td align="center">&nbsp;&nbsp;&nbsp;&nbsp;
                            <font face="Verdana" size="2">
                            <asp:Button OnClick="ShowReserved" autopostback="true" Text="Show Reserved" Font-Bold="true" ID="ShowReservedButton" runat="server" />&nbsp;
                            <asp:Button OnClick="ShowICalFeedLink" autopostback="true" Text="Get iCal Feed" Font-Bold="true" ID="ShowICalFeedLinkButton" runat="server"/>
                            Team: 
                            <asp:DropDownList ID="teamMenu" AutoPostBack="True" OnSelectedIndexChanged="OnTeamMenuItemChanged" runat="server"><asp:ListItem Value="">--- Unauthorized ---</asp:ListItem></asp:DropDownList>
                        </td>
                        <td align="center">&nbsp;&nbsp;&nbsp;&nbsp;
                            <asp:DropDownList runat="server" Height="25px" Width="100px" ID="monthMenu">
                                <asp:ListItem Value="02">February</asp:ListItem>
                                <asp:ListItem Value="03">March</asp:ListItem>
                                <asp:ListItem Value="04">April</asp:ListItem>
                                <asp:ListItem Value="05">May</asp:ListItem>
                                <asp:ListItem Value="06">June</asp:ListItem>
                                <asp:ListItem Value="07">July</asp:ListItem>
                                <asp:ListItem Value="08">Aug</asp:ListItem>
                                <asp:ListItem Value="09">Sep</asp:ListItem>
                                <asp:ListItem Value="10">Oct</asp:ListItem>
                                <asp:ListItem Value="11">Nov</asp:ListItem>
                                <asp:ListItem Value="12">Dec</asp:ListItem>
                            </asp:DropDownList>
                            <asp:DropDownList runat="server" Height="25px" Width="50px" ID="dayMenu">
                                <asp:ListItem Value="01">1</asp:ListItem>
                                <asp:ListItem Value="02">2</asp:ListItem>
                                <asp:ListItem Value="03">3</asp:ListItem>
                                <asp:ListItem Value="04">4</asp:ListItem>
                                <asp:ListItem Value="05">5</asp:ListItem>
                                <asp:ListItem Value="06">6</asp:ListItem>
                                <asp:ListItem Value="07">7</asp:ListItem>
                                <asp:ListItem Value="08">8</asp:ListItem>
                                <asp:ListItem Value="09">9</asp:ListItem>
                                <asp:ListItem Value="10">10</asp:ListItem>
                                <asp:ListItem Value="11">11</asp:ListItem>
                                <asp:ListItem Value="12">12</asp:ListItem>
                                <asp:ListItem Value="13">13</asp:ListItem>
                                <asp:ListItem Value="14">14</asp:ListItem>
                                <asp:ListItem Value="15">15</asp:ListItem>
                                <asp:ListItem Value="16">16</asp:ListItem>
                                <asp:ListItem Value="17">17</asp:ListItem>
                                <asp:ListItem Value="18">18</asp:ListItem>
                                <asp:ListItem Value="19">19</asp:ListItem>
                                <asp:ListItem Value="20">20</asp:ListItem>
                                <asp:ListItem Value="21">21</asp:ListItem>
                                <asp:ListItem Value="22">22</asp:ListItem>
                                <asp:ListItem Value="23">23</asp:ListItem>
                                <asp:ListItem Value="24">24</asp:ListItem>
                                <asp:ListItem Value="25">25</asp:ListItem>
                                <asp:ListItem Value="26">26</asp:ListItem>
                                <asp:ListItem Value="27">27</asp:ListItem>
                                <asp:ListItem Value="28">28</asp:ListItem>
                                <asp:ListItem Value="29">29</asp:ListItem>
                                <asp:ListItem Value="30">30</asp:ListItem>
                                <asp:ListItem Value="31">31</asp:ListItem>
                            </asp:DropDownList>
                            <asp:Button OnClick="ShowAvailable" autopostback="true" Text="Show Available By Date" Font-Bold="true" ID="ShowAvailableButton" runat="server" />
                            &nbsp;&nbsp;&nbsp;&nbsp;
                        </td>
                        <td align="center">&nbsp;&nbsp;&nbsp;&nbsp;
                            <asp:DropDownList runat="server" Height="25px" Width="80px" ID="fieldMenu">
                                <asp:ListItem Value="">----</asp:ListItem>
                            </asp:DropDownList>
                            <asp:Button OnClick="ShowAvailableByField" autopostback="true" Text="Show Available By Field" Font-Bold="true" ID="ShowAvaliableByFieldButton" runat="server" />
                            &nbsp;&nbsp;&nbsp;&nbsp;
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
        <tr>
            <td>
                <div runat="server" ID="divAdminFunctions">
                    <table cellspacing="0" bordercolor="black" bgcolor="#acacac">
                        <tr>
                            <td style="padding-right: .5in">
                                Administrative Functions</td>
                            <td><a href="admin.aspx">Admin functions</a></td>
                    </table>
                </div>
            </td>
        </tr>
        <tr>
            <td style="text-align: center">
                <a href="ReleaseNotes.aspx" target="_blank">release notes</a>
            </td>
        </tr>
        <tr>
            <td>
                <div runat="server" ID="divCalendarFeedLink" style="padding-left: 5pt; padding-right: 5pt; border: black 1pt solid; background: lightgray;">
                    <p>To subscripe to your calendar, you will need to the internet address for the iCalendar feed ("ics" or "iCal" feed).</p>
                    <p>Copy and paste this internet address as your iCalendar feed: <br /><asp:TextBox ID="txtCalendarFeedLink" runat="server" Width="768"/></p>
                    <p>If you need information on adding an internet calendar feed to Outlook 2016, click <a href="icsfeed_howto.html" target="_blank">here</a></p>
                    <p style="text-align:right"><asp:Button OnClick="HideCalendarFeedLink" ID="HideCalendarFeedLinkBotton" runat="server" Text="Got it!" /></p>
                </div>
            </td>
        </tr>
    </table>
</center>
<center>
    <font face="Times" size="4">
        <asp:Label ID="Message2" align="Center" Font-Bold="True" Width="98%" runat="server" />
    </font>
    <asp:DataGrid ID="DataGrid1" BorderColor="black" BorderWidth="1" CellPadding="3"
        Wrap="False" Font-Name="Verdana" Font-Size="8pt" Font-Bold="True" AllowSorting="true"
        OnEditCommand="DataGrid_Edit" OnItemDataBound="Item_Bound" OnItemCommand="DataGrid_Command"
        OnSortCommand="SortCommand" AutoGenerateColumns="false" runat="server">
        <Columns>
            <asp:EditCommandColumn EditText="[*]" CancelText="Release" UpdateText="Reserve" HeaderText="UPDATE">
                <ItemStyle Wrap="False" HorizontalAlign="Center"></ItemStyle>
                <HeaderStyle HorizontalAlign="Center" BackColor="black" ForeColor="white"></HeaderStyle>
            </asp:EditCommandColumn>
            <asp:BoundColumn HeaderText="IsEnabled" ReadOnly="True" Visible="false" DataField="IsEnabled"
                SortExpression="IsEnabled">
                <ItemStyle HorizontalAlign="Center"></ItemStyle>
                <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
            </asp:BoundColumn>
            <asp:BoundColumn HeaderText="SlotNo" ReadOnly="True" Visible="false" DataField="SlotNo"
                SortExpression="SlotNo">
                <ItemStyle HorizontalAlign="Center"></ItemStyle>
                <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
            </asp:BoundColumn>
            <asp:BoundColumn HeaderText="Date" ReadOnly="True" DataField="Date" SortExpression="Date">
                <ItemStyle HorizontalAlign="Center"></ItemStyle>
                <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
            </asp:BoundColumn>
            <asp:BoundColumn HeaderText="Weekday" ReadOnly="True" DataField="Weekday">
                <ItemStyle HorizontalAlign="Center"></ItemStyle>
                <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
            </asp:BoundColumn>
            <asp:BoundColumn HeaderText="Start Time" ReadOnly="True" SortExpression="StartTime"
                DataField="StartTime">
                <ItemStyle HorizontalAlign="Center"></ItemStyle>
                <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
            </asp:BoundColumn>
            <asp:BoundColumn HeaderText="End Time" ReadOnly="True" DataField="EndTime">
                <ItemStyle HorizontalAlign="Center"></ItemStyle>
                <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
            </asp:BoundColumn>
            <asp:BoundColumn HeaderText="Hours" ReadOnly="True" DataField="Hours">
                <ItemStyle HorizontalAlign="Center"></ItemStyle>
                <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
            </asp:BoundColumn>
            <asp:BoundColumn HeaderText="Field" ReadOnly="True" SortExpression="Field" DataField="Field">
                <ItemStyle HorizontalAlign="Center"></ItemStyle>
                <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
            </asp:BoundColumn>
            <asp:BoundColumn HeaderText="Venue" ReadOnly="True" SortExpression="Venue" DataField="Venue">
                <ItemStyle HorizontalAlign="left"></ItemStyle>
                <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
            </asp:BoundColumn>
            <asp:BoundColumn HeaderText="Reserved by" ReadOnly="True" SortExpression="Reserved"
                DataField="Reserved">
                <ItemStyle HorizontalAlign="left"></ItemStyle>
                <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
            </asp:BoundColumn>
            <asp:BoundColumn HeaderText="Reserved on" ReadOnly="True" SortExpression="ReservedTime"
                DataField="ReserveDatetime">
                <ItemStyle HorizontalAlign="left"></ItemStyle>
                <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
            </asp:BoundColumn>
        </Columns>
    </asp:DataGrid>
</center>
  <div id="divResults" runat="server"></div>
<br><br><br>
    <div class="notice">
        <h1>NOTICES:  </h1>
        <ul>
        <li>For H5 and H6, A and B fields: A is the infield first, B is the outfield first. 
        <li>For information on the latest changes, click on "Release Notes" above.</li>
        <li>2017 practice slots are loaded. School fields through March 17th are now available.</li>
        <li>Everyone is allowed to reserve 2 fields per day. Yes, there are ways to <b>cheat</b> the system. Please don't do this. If you exceed the 2 fields per day booking rules you are at risk of losing these fields arbitrarily.</li>
        </ul>
    </div>
    </form>
</body>
</html>
