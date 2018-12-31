<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CalendarLink.aspx.cs" Inherits="Rwp.CalendarLinkPage" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <link rel="stylesheet" href="rwp.css"/>
  <title></title>
</head>
<body>
  <div class="masthead">calendar links</div>
  <form id="form1" runat="server">
    <div>
      <asp:ImageButton ID="LoginOutButton" runat="server" ImageUrl="signin.png" CssClass="loginButton" />
      <table>
        <tr>
          <td>
            <div>
              <asp:DropDownList ID="teamMenu" AutoPostBack="True" OnSelectedIndexChanged="OnTeamMenuItemChanged" runat="server"><asp:ListItem Value="">--- Unauthorized ---</asp:ListItem></asp:DropDownList>

              <p>To subscribe to your calendar, you will need an internet address for the iCalendar feed ("ics" or "iCal" feed).</p>
              <p>
                This calendar link issue issued specifically to you and can be revoked by an administrator. Please make sure you enter a comment below so 
                administrators can identify which links have been issued (e.g. "Link created for Sports Engine website" or "Created link for 
                Majors Red Sox parents"). Every time you click "Create Link" below, a new link will be generated. If you want to create a separate
                link for every parent on your team, you can do this by changing the comment clicking "CreateLink" once for every parent.
              </p>
              <p>
                NOTE: Anyone with this link can access the calendar feed, so if someone shares the link you give them, then every person they
                share the link with will be able to access the calendar link. You can revoke a link at any time if you think it is being misused.
              </p>
              <p>
                Comment for calendar link: <asp:TextBox ID="txtComment" runat="server" width="128"></asp:TextBox>
                <asp:Button ID="btnCreateLink" runat="server" Text="Create Link" OnClick="DoCreateLink" />

              </p>
                Copy and paste this internet address as your iCalendar feed:
                <br />
                <asp:TextBox ID="txtCalendarFeedLink" runat="server" Width="768" />
              </p>
              <p>If you need information on adding an internet calendar feed to Outlook 2016, click <a href="icsfeed_howto.html" target="_blank">here</a></p>
              <p style="text-align: right">
              </p>
            </div>
            <div runat="server" ID="divResults"></div>
            </td>
        </tr>
        <tr>
          <td>
            <asp:DataGrid ID="DataGrid1" runat="server" BorderColor="black" BorderWidth="1" CellPadding="3"
                          Wrap="False" Font-Name="Verdana" Font-Size="8pt" Font-Bold="True" AllowSorting="true" 
                          AutoGenerateColumns="false" >
              <Columns>
                <asp:BoundColumn HeaderText="LinkID" ReadOnly="True" DataField="LinkID"
                                 SortExpression="LinkID">
                  <ItemStyle HorizontalAlign="Center"></ItemStyle>
                  <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
                </asp:BoundColumn>
                <asp:BoundColumn HeaderText="TeamID" ReadOnly="True" DataField="TeamID"
                                 SortExpression="TeamID">
                  <ItemStyle HorizontalAlign="Center"></ItemStyle>
                  <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
                </asp:BoundColumn>
                <asp:BoundColumn HeaderText="Authority" ReadOnly="True" DataField="Authority"
                                 SortExpression="Authority">
                  <ItemStyle HorizontalAlign="Center"></ItemStyle>
                  <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
                </asp:BoundColumn>
                <asp:BoundColumn HeaderText="CreateDate" ReadOnly="True" DataField="CreateDate"
                                 SortExpression="CreateDate">
                  <ItemStyle HorizontalAlign="Center"></ItemStyle>
                  <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
                </asp:BoundColumn>
                <asp:BoundColumn HeaderText="Comment" ReadOnly="True" DataField="Comment"
                                 SortExpression="Comment">
                  <ItemStyle HorizontalAlign="Center"></ItemStyle>
                  <HeaderStyle BackColor="blue" ForeColor="white"></HeaderStyle>
                </asp:BoundColumn>
                <asp:ButtonColumn HeaderText="Revoke" ButtonType="LinkButton" Text="Revoke" CommandName="Delete" />
                <asp:ButtonColumn HeaderText="Get Link" ButtonType="LinkButton" Text="Get Link" CommandName="GetLink" />
              </Columns>
            </asp:DataGrid>
          </td>
        </tr>
    </div>
  </form>
</body>
</html>
