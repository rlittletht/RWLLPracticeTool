<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="admin.aspx.cs" Inherits="Rwp.AdminPage" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link rel="stylesheet" href="rwp.css"/>
    <script language="javascript">
        function DoDownloadTeams()
        {
            window.open("TeamsReport.aspx");
        }
		function DoDownloadSlots()
		{
			window.open("SlotsReport.aspx");
		}
    </script>
</head>
<body>
    <div class="masthead">admin services</div>


    <form id="form1" runat="server">
        <asp:ImageButton ID="LoginOutButton" runat="server" ImageUrl="signin.png" CssClass="loginButton" />
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
				<td colspan=2>
					Results:
					<div class="ErrorReport" id="divError" runat="server" style="width:600px;"></div>

			</tr>
        </table>
    </div>
	</form>

</body>
</html>
