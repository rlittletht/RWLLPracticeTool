<html>
<head>
    <link rel="stylesheet" href="rwp.css"/>
    <title>Redmond West Little League Practice Scheduler V1.9</title>

    <%@ page language="VB" autoeventwireup="True" debug="true" %>

    <%@ import namespace="System.Data" %>
    <%@ import namespace="System.Data.OleDb" %>
    <%@ import namespace="System.Data.SqlClient" %>
    <%@ import namespace="System.Drawing.Color" %>
    <%@ import namespace="System.Security.Permissions" %>
    <script runat="server">
	
 
        Dim sqlPwd As String = "sa"
        ' used for connecting to our DB
        Dim masterPwd As String = "detour/2513"
        ' used for administrator bypass - log in as any team as admin
        Dim subMasterPwd As String = "detour/4016"
        ' allows logging in as any team but not as administrator
        ' detour = bypass; 23646 = admin; 6676 = norm
        
        Dim sqlStrSorted As String
        Dim sqlStrBase As String
        Dim conClsf As SqlConnection
        Dim cmdMbrs As SqlCommand
        Dim rdrMbrs As SqlDataReader
        
        Dim loggedIn As Boolean = False
        Dim loggedInAsAdmin As Boolean = False
        Dim teamName As String
        Dim teamNameForAvailableSlots As String
        ' team name used to query for reserved and available slots

        Dim showingReserved As Boolean = True
        Dim showingAvailableByField As Boolean = False
        Dim DBConn As SqlConnection '  = New SqlConnection(sSqlConnectionString)
// This line contained a SECRET and was automatically sanitized. This file will probably not compile now. Contact original author for the secret line
        '   Dim DBConn As SqlConnection = New SqlConnection("server=cacofonix; initial catalog=db0902;trusted_connection=yes")

        
        Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
            Dim sSqlConnectionString As String
            Dim rootWebConfig As System.Configuration.Configuration
        
            sSqlConnectionString = ""
        
            rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/Rwp2")
        
            Dim connString As System.Configuration.ConnectionStringSettings
        
            If (rootWebConfig.ConnectionStrings.ConnectionStrings.Count > 0) Then
                connString = rootWebConfig.ConnectionStrings.ConnectionStrings("dbSchedule")
                If Not (connString Is Nothing) Then
                    sSqlConnectionString = connString.ConnectionString
                End If
            End If
        
            DBConn = New SqlConnection(sSqlConnectionString)
            
            Message0.Text = "Redmond West Little League Practice Scheduler v1.9 (Server Date = " + DateTime.UtcNow.AddHours(-8).Date + ")"
            Try
                teamName = teamMenu.SelectedItem.Text
                teamNameForAvailableSlots = teamMenu.SelectedItem.Text
            
                ' this teams reservations
                'sqlStrBase = "exec usp_DisplaySlotsEx '" + teamName + "',1,'00/00/00'"
                '			DataGrid1.Columns(0).HeaderText = "Release"
 
                ' ViewState variables
                ' In every other place where we change any of these vraibles,
                ' we must set it in ViewState as well.

                If ViewState("sqlStrBase") = Nothing Or CStr(ViewState("sqlStrBase")) = "" Then
                    sqlStrBase = "exec usp_DisplaySlotsEx '" + teamName + "',1,'00/00/00'," + "''"
                Else
                    sqlStrBase = CStr(ViewState("sqlStrBase"))
                End If
                sqlStrSorted = sqlStrBase + ",Date"
       
                If ViewState("showingReserved") = Nothing Then
                    showingReserved = True
                    ViewState("showingReserved") = True
                Else
                    showingReserved = CBool(ViewState("showingReserved"))
                End If
                
                If ViewState("showingAvailableByField") = Nothing Then
                    showingAvailableByField = False
                Else
                    showingAvailableByField = CBool(ViewState("showingAvailableByField"))
                End If
        
                If ViewState("loggedIn") = Nothing Then
                    loggedIn = False
                Else
                    loggedIn = CBool(ViewState("loggedIn"))
                End If
        
                If ViewState("loggedInAsAdmin") = Nothing Then
                    loggedInAsAdmin = False
                Else
                    loggedInAsAdmin = CBool(ViewState("loggedInAsAdmin"))
                End If
            
                If loggedInAsAdmin Then
                    teamNameForAvailableSlots = "Administrator"
                End If

                If Not IsPostBack Then
                    DBConn.Open()
                    cmdMbrs = DBConn.CreateCommand
                    'populate the teamMenu
                    sqlStrSorted = "exec usp_PopulateTeamList"
                    cmdMbrs.CommandText = sqlStrSorted
                    rdrMbrs = cmdMbrs.ExecuteReader
                    teamMenu.DataSource = rdrMbrs
                    teamMenu.DataTextField = "TeamName"
                    teamMenu.DataValueField = "TeamName"
                    teamMenu.DataBind()
                    rdrMbrs.Close()
                    'populate the fieldMenu
                    sqlStrSorted = "exec usp_PopulateFieldList"
                    cmdMbrs.CommandText = sqlStrSorted
                    rdrMbrs = cmdMbrs.ExecuteReader
                    fieldMenu.DataSource = rdrMbrs
                    fieldMenu.DataTextField = "Field"
                    fieldMenu.DataValueField = "Field"
                    fieldMenu.DataBind()
                    rdrMbrs.Close()
                    cmdMbrs.Dispose()
                    DBConn.Close()
                End If
            Catch ex As Exception
                Message0.Text = ex.Message
            End Try
            
        End Sub

        Sub BindGrid()
            DBConn.Open()
            cmdMbrs = DBConn.CreateCommand
            cmdMbrs.CommandText = sqlStrSorted
            rdrMbrs = cmdMbrs.ExecuteReader
            DataGrid1.DataSource = rdrMbrs
            DataGrid1.DataBind()
            rdrMbrs.Close()
            cmdMbrs.Dispose()
            DBConn.Close()
        End Sub
        
        

        Sub LogOff(ByVal sender As Object, ByVal e As EventArgs)
            Message1.Text = ""
            loggedIn = False
            ViewState("loggedIn") = loggedIn
            passwordTextBox.Text = ""
            teamMenu.Enabled = "true"
            loggedInAsAdmin = False
            ViewState("loggedInAsAdmin") = loggedInAsAdmin
            ViewState("sqlStrBase") = ""
            RunQuery(sender, e)
        End Sub

        Sub ValidateLogin(ByVal sender As Object, ByVal e As EventArgs)
            Dim sqlStrLogin As String
            Dim temp As String
     
            ViewState("sqlStrBase") = ""

            If teamName.Contains("'") Or passwordTextBox.Text.Contains("'") Then
                temp = "-1" ' prevent SQL injection; no password contains an apostrophe
            Else
                DBConn.Open()
                If passwordTextBox.Text = masterPwd Or passwordTextBox.Text = subMasterPwd Then
                    sqlStrLogin = "SELECT count(*) as Count from rwllTeams where TeamName = '" + teamName + "'"
                Else
                    sqlStrLogin = "SELECT count(*) as Count from rwllTeams where TeamName = '" + teamName + "' and PW = '" + passwordTextBox.Text + "'"
                End If
                cmdMbrs = DBConn.CreateCommand
                cmdMbrs.CommandText = sqlStrLogin
                rdrMbrs = cmdMbrs.ExecuteReader
                temp = "-1"
                While rdrMbrs.Read()
                    temp = rdrMbrs(0)
                End While
			
                rdrMbrs.Close()
                cmdMbrs.Dispose()
                DBConn.Close()
            End If
 
            If temp = "1" Then
                Message1.Text = "Login successful"
                loggedIn = True
                ViewState("loggedIn") = loggedIn
                teamMenu.Enabled = "false"
                Message1.ForeColor = System.Drawing.Color.Green
                Message2.Text = ""
                DataGrid1.Columns(0).HeaderText = "Release"
                sqlStrBase = "exec usp_DisplaySlotsEx '" + teamName + "',1,'2013-01-01'," + "''"
                sqlStrSorted = sqlStrBase + ",Date"
                ViewState("sqlStrBase") = sqlStrBase
                BindGrid()
                If passwordTextBox.Text = masterPwd Then
                    loggedInAsAdmin = True
                    ViewState("loggedInAsAdmin") = loggedInAsAdmin
                End If
            Else
                teamMenu.Enabled = "true"
                Message1.Text = "Incorrect Password"
                loggedIn = False
                ViewState("loggedIn") = loggedIn
                Message1.ForeColor = System.Drawing.Color.Red
                loggedInAsAdmin = False
                ViewState("loggedInAsAdmin") = loggedInAsAdmin
            End If
        End Sub
   
        Sub ShowReserved(ByVal sender As Object, ByVal e As EventArgs)
            Try
                
                showingReserved = True
                showingAvailableByField = False
                ViewState("showingReserved") = showingReserved
                ViewState("showingAvailableByField") = showingAvailableByField
                RunQuery(sender, e)
            Catch ex As Exception
                Message0.Text = Message0.Text
            End Try
        End Sub
        
        Sub ShowAvailable(ByVal sender As Object, ByVal e As EventArgs)
            Try
                showingAvailableByField = False
                showingReserved = False
                ViewState("showingReserved") = showingReserved
                ViewState("showingAvailableByField") = showingAvailableByField
                RunQuery(sender, e)
            Catch ex As Exception
                Message0.Text = Message0.Text
            End Try
        End Sub
        
        Sub RunQuery(ByVal sender As Object, ByVal e As EventArgs)
            Message2.Text = ""
            If loggedIn Then
                If showingReserved Then
                    DataGrid1.Columns(0).HeaderText = "Release"
                    ViewState("showingReserved") = True
                    sqlStrBase = "exec usp_DisplaySlotsEx '" + teamName + "',1,'00/00/00'," + "''"
                    sqlStrSorted = sqlStrBase + ",Date"
                    ViewState("sqlStrBase") = sqlStrBase
                Else
                    DataGrid1.Columns(0).HeaderText = "Reserve"
                    ViewState("showingReserved") = False
                    '*********************************                    
                    'DBConn.Open()
                    'cmdMbrs = DBConn.CreateCommand
                    'cmdMbrs.CommandText = "exec usp_ReservedByDate '" + teamName + "','" + monthMenu.SelectedItem.Value + "/" + dayMenu.SelectedItem.Value + "/2013'"
                    'Dim i As Integer
                    'i = Convert.ToInt32(cmdMbrs.ExecuteScalar())
                    'cmdMbrs.Dispose()
                    'DBConn.Close()
                    'If i = 0 Then
                    '    ViewState("ReserveAtSelectedDate") = True
                    'Else
                    '    ViewState("ReserveAtSelectedDate") = False
                    'End If
                    '*********************************
                    If showingAvailableByField Then
                        ViewState("showingAvailableByField") = True
                        sqlStrBase = "exec usp_DisplaySlotsEx '" + teamNameForAvailableSlots + "',2,'" + monthMenu.SelectedItem.Value + "/" + dayMenu.SelectedItem.Value + "/2013','" + fieldMenu.SelectedItem.Value + "'"
                        sqlStrSorted = sqlStrBase + ",Date"
                    Else
                        sqlStrBase = "exec usp_DisplaySlotsEx '" + teamNameForAvailableSlots + "',2,'" + monthMenu.SelectedItem.Value + "/" + dayMenu.SelectedItem.Value + "/2013'," + "''"
                        sqlStrSorted = sqlStrBase + ",Date"
                    End If
                    
                    ViewState("sqlStrBase") = sqlStrBase
                End If
                DataGrid1.EditItemIndex = -1
            Else
                If showingReserved And Not teamName.Contains("--") Then
                    DataGrid1.Columns(0).HeaderText = ""
                    sqlStrBase = "exec usp_DisplaySlotsEx '" + teamName + "',1,'00/00/00'," + "''"
                    sqlStrSorted = sqlStrBase + ",Date"
                    ViewState("sqlStrBase") = sqlStrBase
                Else
                    ' show available slots for a given day
                    DataGrid1.Columns(0).HeaderText = ""
                    If showingAvailableByField Then
                        sqlStrBase = "exec usp_DisplaySlotsEx 'ShowAll',0,'" + monthMenu.SelectedItem.Value + "/" + dayMenu.SelectedItem.Value + "/2013','" + fieldMenu.SelectedItem.Value + "'"
                        sqlStrSorted = sqlStrBase + ",Date"
                    Else
                        sqlStrBase = "exec usp_DisplaySlotsEx 'ShowAll',0,'" + monthMenu.SelectedItem.Value + "/" + dayMenu.SelectedItem.Value + "/2013'," + "''"
                        sqlStrSorted = sqlStrBase + ",Date"
                    End If
                    
                    ViewState("sqlStrBase") = sqlStrBase
                End If

                Message2.ForeColor = System.Drawing.Color.Green
                Message2.Text = "You must login to reserve fields."
            End If
            BindGrid()
        End Sub

        Sub SortCommand(ByVal sender As Object, ByVal e As DataGridSortCommandEventArgs)
            sqlStrSorted = sqlStrBase + "," + e.SortExpression
            'If showingReserved Then
            '   sqlStrSorted = "exec usp_DisplaySlotsEx '" + teamName + "',1,'00/00/00'," + e.SortExpression
            'Else
            '   sqlStrSorted = "exec usp_DisplaySlotsEx '" + teamNameForAvailableSlots + "',2,'" + monthMenu.SelectedItem.Value + "/" + dayMenu.SelectedItem.Value + "/2013'," + e.SortExpression
            'End If
            BindGrid()
        End Sub
 

        Sub DataGrid_Command(ByVal sender As Object, ByVal e As DataGridCommandEventArgs)
            Select Case (CType(e.CommandSource, LinkButton)).CommandName
                Case "Delete"
                    DeleteItem(e)
                Case Else
                    ' Do nothing.
            End Select
        End Sub



        Sub DataGrid_Cancel(ByVal sender As Object, ByVal e As DataGridCommandEventArgs)
        End Sub

        Sub DataGrid_Edit(ByVal sender As Object, ByVal e As DataGridCommandEventArgs)
            Dim SQLcmd As String
            Dim temp As String
			
            DataGrid1.EditItemIndex = e.Item.ItemIndex
            If DataGrid1.Columns(0).HeaderText = "Release" Then
                DBConn.Open()
                SQLcmd = "exec usp_UpdateSlots 'Rel', '" + teamName + "','" + e.Item.Cells(2).Text + "'" '+ "','" + e.Item.Cells(4).Text + "','" + e.Item.Cells(1).Text + "','" + e.Item.Cells(5).Text + "'"
                cmdMbrs = DBConn.CreateCommand
                cmdMbrs.CommandText = SQLcmd
                rdrMbrs = cmdMbrs.ExecuteReader
                temp = "-1"
                While rdrMbrs.Read()
                    temp = rdrMbrs(0)
                End While
                If temp = "0" Then
                    Message2.Text = "Release successful"
                    Message2.ForeColor = System.Drawing.Color.Green
                End If
                If temp = "-1" Then
                    Message2.Text = "Error releasing field"
                    Message2.ForeColor = System.Drawing.Color.Red
                End If
                DataGrid1.EditItemIndex = -1
                rdrMbrs.Close()
                cmdMbrs.Dispose()
                DBConn.Close()
            End If
			
            If DataGrid1.Columns(0).HeaderText = "Reserve" Then
                DBConn.Open()
                If loggedInAsAdmin Then
                    SQLcmd = "exec usp_UpdateSlots 'ResAdmin'"
                Else
                    SQLcmd = "exec usp_UpdateSlots 'Res'"
                End If
                SQLcmd = SQLcmd + ", '" + teamName + "','" + e.Item.Cells(2).Text + "'" '+ "','" + e.Item.Cells(4).Text + "','" + e.Item.Cells(2).Text + "','" + e.Item.Cells(5).Text + "'"
                cmdMbrs = DBConn.CreateCommand
                cmdMbrs.CommandText = SQLcmd
                rdrMbrs = cmdMbrs.ExecuteReader
                temp = "-3"
                If rdrMbrs.Read() Then
                    temp = rdrMbrs(0)
                End If
                If temp = "0" Then
                    Message2.Text = "Reservation successful"
                    Message2.ForeColor = System.Drawing.Color.Green
                ElseIf temp = "-1" Then
                    Message2.Text = "Field already reserved"
                    Message2.ForeColor = System.Drawing.Color.Red
                ElseIf temp = "-2" Then
                    Message2.Text = "Only two fields can be reserved per day"
                    Message2.ForeColor = System.Drawing.Color.Red
                ElseIf temp = "-3" Then
                    Message2.Text = "Error reserving field"
                    Message2.ForeColor = System.Drawing.Color.Red
                ElseIf temp = "-4" Then
                    Message2.Text = "Only one H5*/H6* can be reserved per week"
                    Message2.ForeColor = System.Drawing.Color.Red
                Else
                    Message2.Text = "Unknown error reserving field (" + temp + ")"
                    Message2.ForeColor = System.Drawing.Color.Red
                End If
                DataGrid1.EditItemIndex = -1
                ' return to list of reserved fields
                DataGrid1.Columns(0).HeaderText = "Release"
                sqlStrBase = "exec usp_DisplaySlotsEx '" + teamName + "',1,'00/00/00'," + "''"
                sqlStrSorted = sqlStrBase + ",Date"
                ViewState("sqlStrBase") = sqlStrBase
                rdrMbrs.Close()
                cmdMbrs.Dispose()
                DBConn.Close()
            End If
            BindGrid()
        End Sub

        Sub DeleteItem(ByVal e As DataGridCommandEventArgs)
        End Sub
        
        Sub Item_Bound(ByVal sender As Object, ByVal e As DataGridItemEventArgs)

            Dim link As LinkButton
            Dim strDateField As String
            Dim dateField As DateTime
            Dim IsEnabled As Boolean
            Dim IsloggedIn As Boolean
            
            IsloggedIn = Convert.ToBoolean(ViewState("loggedIn"))
            
            If e.Item.Cells(0).Controls.Count > 0 Then
                
                link = CType(e.Item.Cells(0).Controls(0), LinkButton)
                strDateField = e.Item.Cells(3).Text
                dateField = Convert.ToDateTime(strDateField)
                If (Len(e.Item.Cells(1).Text) = 0 Or e.Item.Cells(1).Text = "&nbsp;") Then
                    IsEnabled = False
                Else
                    IsEnabled = True
                    link.ToolTip = e.Item.Cells(1).Text
                End If
                '                IsEnabled = Convert.ToBoolean(Convert.ToInt32(e.Item.Cells(1).Text))
                
                If Not IsloggedIn Then
                    link.Enabled = False
                    link.ToolTip = "Not logged in"
                End If
                
                Dim dttmNow As DateTime
                
                
                If Not IsNothing(link) And DateTime.Compare(dateField, DateTime.UtcNow.AddHours(-8).Date) <= 0 And CBool(ViewState("showingReserved")) Then
                    link.Enabled = False
                    link.ToolTip = "date has passed: " + dateField + " < " + DateTime.UtcNow.AddHours(-8).Date
                End If
                
                If Not IsNothing(link) And IsEnabled And Not CBool(ViewState("showingReserved")) Then
                    link.Enabled = False
                End If
            End If
            
        End Sub
        
        

        Sub ShowAvailableByField(sender As Object, e As System.EventArgs)
            Try
                showingReserved = False
                showingAvailableByField = True
                ViewState("showingReserved") = showingReserved
                ViewState("showingAvailableByField") = showingAvailableByField
                RunQuery(sender, e)
            Catch ex As Exception
                Message0.Text = Message0.Text
            End Try
        End Sub
    </script>
</head>
<body style="height: 244">
    <form id="Form1" runat="server" style="text-align: center" enctype="multipart/form-data">
    <p>
        <font face="Times" size="4">
            <asp:Label ID="Message0" align="Center" Font-Bold="True" BackColor="gold" Width="100%"
                runat="server" />
        </font>
    </p>
    <asp:DropDownList runat="server" Height="25px" Width="180px" ID="teamMenu">
        <asp:ListItem Value="">-- Select Team --</asp:ListItem>
    </asp:DropDownList>
    <font face="Verdana" size="2">&nbsp; &nbsp; &nbsp; Login:</font>
    <asp:TextBox ID="passwordTextBox" TextMode="password" Columns="9" runat="server" />
    &nbsp;
    <asp:Button OnClick="ValidateLogin" autopostback="true" Text="Login" Font-Bold="true"
        ID="loginButton" runat="server" />
    <asp:Button OnClick="LogOff" autopostback="true" Text="Logoff" Font-Bold="true" ID="logoffButton"
        runat="server" />
           <a href="ReleaseNotes.aspx" target="_Blank">release notes</a>
    <font face="Verdana" size="3">
        <asp:Label ID="Message1" align="Center" Width="10%" runat="server" />
    </font>
     
<center><table border="1" cellpadding="3" cellspacing="0" bordercolor="black" bgcolor="#acacac"><tr>
	<td align="center">&nbsp;&nbsp;&nbsp;&nbsp;
        <font face="Verdana" size="2">
            <asp:Button OnClick="ShowReserved" autopostback="true" Text="Show Reserved" Font-Bold="true"
                ID="ShowReservedButton" runat="server" />
     &nbsp;&nbsp;&nbsp;&nbsp;</td><td align="center">&nbsp;&nbsp;&nbsp;&nbsp;
        <font face="Verdana" size="2">Month:</font>
        <asp:DropDownList runat="server" Height="25px" Width="100px" ID="monthMenu">
            <asp:ListItem Value="02">February</asp:ListItem>
            <asp:ListItem Value="03">March</asp:ListItem>
            <asp:ListItem Value="04">April</asp:ListItem>
            <asp:ListItem Value="05">May</asp:ListItem>
            <asp:ListItem Value="06">June</asp:ListItem>
        </asp:DropDownList>
        <font face="Verdana" size="2">Day:</font>
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
        <asp:Button OnClick="ShowAvailable" autopostback="true" Text="Show Available By Date"
            Font-Bold="true" ID="ShowAvailableButton" runat="server" />
     &nbsp;&nbsp;&nbsp;&nbsp;</td><td align="center">&nbsp;&nbsp;&nbsp;&nbsp;
            <font face="Verdana" size="2">Field:</font>
        <asp:DropDownList runat="server" Height="25px" Width="180px" ID="fieldMenu">
            <asp:ListItem Value="">-- Select Field --</asp:ListItem>
        </asp:DropDownList>
        <asp:Button OnClick="ShowAvailableByField" autopostback="true" Text="Show Available By Field"
            Font-Bold="true" ID="ShowAvaliableByFieldButton" runat="server" />
     &nbsp;&nbsp;&nbsp;&nbsp;</td>
</tr></table></center><br><br>
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
        </Columns>
    </asp:DataGrid></form>
</center>
<br><br><br>
    <div class="notice">
        <h1>NOTICES:  </h1>
        <ul>
        <li>Everyone is allowed to reserve 2 fields per day. Yes, there are ways to <b>cheat</b> the system. Please don't do this. If you exceed the 2 fields per day booking rules you are at risk of losing these fields arbitrarily.</li>
            <li>School fields are available for reservations</li>
        <li>For information on the latest changes, click on "Release Notes" above.</li>
        </ul>
    </div>
</body>
</html>