<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="admin.aspx.cs" Inherits="Rwp.AdminPage" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link rel="stylesheet" href="rwp.css"/>
    <script language="javascript">
        function DoDownloadTeams()
        {
            window.open("TeamsReport.aspx", "_blank");
        }
		function DoDownloadSlots()
		{
			window.open("SlotsReport.aspx", "_blank");
		}
    </script>
</head>
<body>
  <form id="form1" runat="server">
    <div class="mastHeadButtons">
      <asp:ImageButton ID="GoHome" runat="server" ImageUrl="home.png" />
      <asp:ImageButton ID="LoginOutButton" runat="server" ImageUrl="signin.png" />
    </div>
    <div class="masthead">admin services</div>
    <div>
        <p>Annual cleanup</p>
        <p>client ip address: <span id="ipClient" runat="server"></span> </p>
        <table>
            <tr>
				<td>
				 <asp:Button ID="btnDownloadTeams" runat="server" Text="Download Team Logins" OnClick="EnableClearItems" OnClientClick="DoDownloadTeams()" />
				</td>
				<td>
					<p>Download all of the team login information into a csv file.  You MUST complete this step before ClearTeams is enabled below</p>
				</td>
			</tr>
			<tr>
				<td>
				 <asp:Button ID="btnClearTeams" runat="server" Text="Delete Team Logins" OnClick="DoDeleteTeams" Enabled="False" />
				</td>
				<td>
					<p>Clear out all of the team logins.  This will NOT delete the Administrator login</p>
				</td>
			</tr>
			<tr>
				<td>
                    <asp:FileUpload ID="fuTeams" runat="server" />
				 <asp:Button ID="btnUploadTeams" runat="server" Text="Upload Teams" OnClick="DoUploadTeams" />
                    
				</td>
				<td>
					<p>Upload the given teams to the database.  This will automatically generate passwords if the password field is blank.</p>
				</td>
			</tr>
            <tr>
				<td>
				 <asp:Button ID="btnDownloadSlots" runat="server" Text="Download Slots" OnClick="EnableDeleteSlots" OnClientClick="DoDownloadSlots()" />
				</td>
				<td>
					<p>Download all of the team login information into a csv file.  You MUST complete this step before ClearTeams is enabled below</p>
				</td>
			</tr>
			<tr>
				<td>
				 <asp:Button ID="btnClearAllSlots" runat="server" Text="Delete ALL Slots" OnClick="DoDeleteSlots" Enabled="False" />
				</td>
				<td>
					<p>This will delete *ALL* slots form the database.  BE CAREFUL!</p>
				</td>
			</tr>
			<tr>
				<td>
				 <asp:Button ID="btnClearLastYear" runat="server" Text="Delete 2014 Slots" OnClick="DoDelete2014Slots" Enabled="False" />
				</td>
				<td>
					<p>Delete all of the slots from 2014.</p>
				</td>
			</tr>
			<tr>
				<td>
                    <asp:FileUpload ID="fuSlots" runat="server" />
				 <asp:Button ID="btnUploadSlots" runat="server" Text="Upload Slots" OnClick="DoUploadSlots" />
                    
				</td>
				<td>
					<p>Upload the given slots to the database.</p>
				</td>
			</tr>
      <tr>
        <td>
          <asp:Button ID="btnAddUser" runat="server" Text="Add User" OnClick="DoShowAddUser" />
        </td>
        <td>
          <p>Add a user/team association</p>
        </td>
      </tr>
      <tr runat="server" id="rowAddUser">
        <td>
          <asp:Button ID="btnDoAddUser" runat="server" Text="Add" OnClick="DoAddUser" />
          <asp:Button ID="btnCancelAddUser" runat="server" Text="Cancel" OnClick="CancelAddUser" />
        </td>
        <td>
          <nobr>CreateTeam? <asp:CheckBox runat="server" id="chkAddTeam"/></nobr>
          <nobr>Identity (email): <asp:TextBox runat="server" id="txtAddIdenity"></asp:TextBox></nobr>
          <nobr>Tenant: <asp:TextBox runat="server" id="txtAddTenant" width="3in">9188040d-6c67-4c5b-b112-36a304b66dad</asp:TextBox></nobr>
          <nobr>Team Name: <asp:TextBox runat="server" id="txtAddTeamName"></asp:TextBox></nobr>
          <nobr>Division: <asp:TextBox runat="server" id="txtAddDivision" width=".25in"></asp:TextBox></nobr>
          <nobr>Email (blank OK): <asp:TextBox runat="server" id="txtAddEmail"></asp:TextBox></nobr>
        </td>
      </tr>
			<tr>
				<td colspan=2>
					Results:
					<div class="ErrorReport" id="divError" runat="server" style="width:600px;"></div>

			</tr>
        </table>
    </div>
	</form>

</body>
</html>
