<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="RedeemInvitation.aspx.cs" Inherits="Rwp.RedeemInvitation" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<script runat="server">

</script>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Redeem Invitation</title>
    <link rel="stylesheet" href="rwp.css"/>
</head>
<body>
<form id="htmlForm" runat="server">
    <div class="mastHeadButtons">
        <asp:ImageButton ID="GoHome" runat="server" ImageUrl="home.png"/>
        <asp:ImageButton ID="LoginOutButton" runat="server" ImageUrl="signin.png"/>
    </div>
    <div class="masthead">redeem invitation code</div>
    <div>
        <table class="layout">
            <tr>
                <td>Primary Identity:</td>
                <td><asp:TextBox ID="txtPrimaryIdentity" runat="server" Width="368" Enabled="False" /></td>
            </tr>
            <tr>
                <td>Tenant ID:</td>
                <td><asp:TextBox ID="txtTenantId" runat="server" Width="256" Enabled="False"/></td>
            </tr>
            <tr>
                <td>Invitation Code:</td>
                <td><asp:TextBox ID="txtInvitationCode" runat="server" Width="72"/></td>
            </tr>
            <tr>
                <td colspan="2" align="right"><asp:Button runat="server" ID="btnRedeem" Text="Redeem Code" OnClick="DoRedeem"/></td>
            </tr>
            <tr>
                <td colspan=2>
                    <div class="ErrorReport" id="divError" runat="server" style="width: 600px;"></div>
                </td>
            </tr>
        </table>
    </div>
</form>
</body>
</html>