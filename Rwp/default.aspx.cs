using System;
using Microsoft.Owin;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.EnterpriseServices;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Web;
using Microsoft.Owin.Security.Notifications;
using Owin;
using TCore;

namespace Rwp
{
    public partial class default1 : System.Web.UI.Page
    {
        void FillInCalendarLink()
        {
            string s;

            s = teamMenu.Text.Replace(" ", "%20");

            txtCalendarFeedLink.Text = $"http://rwllpractice.azurewebsites.net/icsfeed.aspx?Team={s}";
        }

        private SqlConnection DBConn;
        private string sqlStrSorted;
        private string sqlStrBase;
        private SqlConnection conClsf;
        private SqlCommand cmdMbrs;
        private SqlDataReader rdrMbrs;

        private bool loggedIn = false;
        private bool loggedInAsAdmin = false;
        private string teamName;
        private string teamNameForAvailableSlots;
        
        // team name used to query for reserved and available slots
        private bool showingReserved = true;
        private bool showingAvailableByField = false;
        private string sCurYear;

        /// <summary>
        /// Send an OpenID Connect sign-in request.
        /// Alternatively, you can just decorate the SignIn method with the [Authorize] attribute
        /// </summary>
        public void SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                HttpContext.Current.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = "/" },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }

        /// <summary>
        /// Send an OpenID Connect sign-out request.
        /// </summary>
        public void SignOut()
        {
            HttpContext.Current.GetOwinContext().Authentication.SignOut(
                OpenIdConnectAuthenticationDefaults.AuthenticationType,
                CookieAuthenticationDefaults.AuthenticationType);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings["dbSchedule"];
            string sSqlConnectionString = conn.ConnectionString;

            DBConn = new SqlConnection(sSqlConnectionString);

            sCurYear = DateTime.UtcNow.Year.ToString();
            string sIdentity = System.Security.Claims.ClaimsPrincipal.Current.FindFirst("preferred_username")?.Value;
            LoginInfo.Text = sIdentity ?? "Please login to contiue";

            Message0.Text =
                $"Redmond West Little League Practice Scheduler v1.9 (Server DateTime = {DateTime.UtcNow.AddHours(-8)} ({sCurYear})";

            try
            {
                teamName = teamMenu.SelectedItem.Text;
                teamNameForAvailableSlots = teamMenu.SelectedItem.Text;

                // this teams reservations
                //sqlStrBase = "exec usp_DisplaySlotsEx '" + Sqlify(teamName) + "',1,'00 / 00 / 00'"
                //			DataGrid1.Columns(0).HeaderText = "Release"

                // ViewState variables
                // In every other place where we change any of these vraibles,
                // we must set it in ViewState as well.

                if (String.IsNullOrEmpty((string) ViewState["sqlStrBase"]))
                    sqlStrBase = "exec usp_DisplaySlotsEx '" + Sql.Sqlify(teamName) + "',1,'00/00/00'," + "''";
                else
                    sqlStrBase = (string) ViewState["sqlStrBase"];

                sqlStrSorted = sqlStrBase + ",Date";

                if (ViewState["showingReserved"] == null)
                {
                    showingReserved = true;
                    ViewState["showingReserved"] = true;
                }
                else
                {
                    showingReserved = (bool) ViewState["showingReserved"];
                }

                if (ViewState["showingAvailableByField"] == null)
                    showingAvailableByField = false;
                else
                    showingAvailableByField = (bool) ViewState["showingAvailableByField"];

                if (ViewState["loggedIn"] == null)
                    loggedIn = false;
                else
                    loggedIn = (bool) ViewState["loggedIn"];

                if (ViewState["showingCalLink"] == null)
                    ViewState["showingCalLink"] = false;


                divCalendarFeedLink.Visible = (bool)ViewState["showingCalLink"];

                if (ViewState["loggedInAsAdmin"] == null)
                    loggedInAsAdmin = false;
                else
                    loggedInAsAdmin = (bool) ViewState["loggedInAsAdmin"];

                if (loggedInAsAdmin)
                    teamNameForAvailableSlots = "Administrator";

                if (sIdentity != null)
                    LoadPrivs(sIdentity);

                if (!IsPostBack)
                {
                    DBConn.Open();
                    cmdMbrs = DBConn.CreateCommand();
                    // populate the teamMenu
                    sqlStrSorted = "exec usp_PopulateTeamList";
                    cmdMbrs.CommandText = sqlStrSorted;
                    rdrMbrs = cmdMbrs.ExecuteReader();
                    teamMenu.DataSource = rdrMbrs;
                    teamMenu.DataTextField = "TeamName";
                    teamMenu.DataValueField = "TeamName";
                    teamMenu.DataBind();
                    rdrMbrs.Close();
                    // populate the fieldMenu
                    sqlStrSorted = "exec usp_PopulateFieldList";

                    cmdMbrs.CommandText = sqlStrSorted;
                    rdrMbrs = cmdMbrs.ExecuteReader();
                    fieldMenu.DataSource = rdrMbrs;
                    fieldMenu.DataTextField = "Field";
                    fieldMenu.DataValueField = "Field";
                    fieldMenu.DataBind();
                    rdrMbrs.Close();
                    cmdMbrs.Dispose();
                    DBConn.Close();
                }

                FillInCalendarLink();
            }
            catch (Exception exc)
            {
                Message0.Text = exc.Message;
            }
        }

        protected void BindGrid()
        {
            DBConn.Open();
            cmdMbrs = DBConn.CreateCommand();
            cmdMbrs.CommandText = sqlStrSorted;
            rdrMbrs = cmdMbrs.ExecuteReader();
            DataGrid1.DataSource = rdrMbrs;
            DataGrid1.DataBind();
            rdrMbrs.Close();
            cmdMbrs.Dispose();
            DBConn.Close();
        }

        protected void LogOff(object sender, EventArgs e)
        {
            Message1.Text = "";
            loggedIn = false;
            ViewState["loggedIn"] = loggedIn;
            passwordTextBox.Text = "";
            teamMenu.Enabled = true;
            loggedInAsAdmin = false;
            ViewState["loggedInAsAdmin"] = loggedInAsAdmin;
            ViewState["sqlStrBase"] = "";
            RunQuery(sender, e);
            SignOut();
        }

        void LoadPrivs(string sIdentity)
        {
            string sqlStrLogin;
            int temp;

            ViewState["sqlStrBase"] = "";

            DBConn.Open();
            // don't need to validate a password -- once we have an authenticated identity, just get its privileges
            sqlStrLogin = $"SELECT TeamName as Count from rwllTeams where Email1 = '{Sql.Sqlify(sIdentity)}'";
            cmdMbrs = DBConn.CreateCommand();
            cmdMbrs.CommandText = sqlStrLogin;
            rdrMbrs = cmdMbrs.ExecuteReader();
            temp = -1;
            teamName = null;

            while (rdrMbrs.Read())
            {
                teamName = rdrMbrs.GetString(0);
            }

            rdrMbrs.Close();
            cmdMbrs.Dispose();
            DBConn.Close();

            if (teamName != null)
            {
                Message1.Text = $"Welcome to RedmondWest Practice Tool ({sIdentity})...";
                loggedIn = true;
                ViewState["loggedIn"] = loggedIn;
                teamMenu.SelectedValue = teamName;

                teamMenu.Enabled = false;
                Message1.ForeColor = System.Drawing.Color.Green;
                Message2.Text = "";
                DataGrid1.Columns[0].HeaderText = "Release";
                sqlStrBase = "exec usp_DisplaySlotsEx '" + Sql.Sqlify(teamName) + "',1,'" + sCurYear + "-01-01'," +
                             "''";
                sqlStrSorted = sqlStrBase + ",Date";
                ViewState["sqlStrBase"] = sqlStrBase;
                BindGrid();
            }
            else
            {
                teamMenu.Enabled = true;
                Message1.Text = $"User '{sIdentity} not authorized!";
                loggedIn = false;
                ViewState["loggedIn"] = loggedIn;
                Message1.ForeColor = System.Drawing.Color.Red;
                loggedInAsAdmin = false;
                ViewState["loggedInAsAdmin"] = loggedInAsAdmin;
            }

        }

        protected void ValidateLogin(object sender, EventArgs e)
        {
            SignIn();
        }

        protected void ShowICalFeedLink(object sender, EventArgs e)
        {
            divCalendarFeedLink.Visible = true;
            ViewState["showingCalLink"] = true;
            // RunQuery(sender, e)
        }

        protected void HideCalendarFeedLink(object sender, EventArgs e)
        {
            divCalendarFeedLink.Visible = false;
            ViewState["showingCalLink"] = false;
            // RunQuery(sender, e)
        }

        protected void ShowReserved(object sender, EventArgs e)
        {
            try
            {
                showingReserved = true;
                showingAvailableByField = false;
                ViewState["showingReserved"] = showingReserved;
                ViewState["showingAvailableByField"] = showingAvailableByField;
                RunQuery(sender, e);
            }
            catch (Exception ex)
            {
                Message0.Text = Message0.Text + ". Exception " + ex.Message;
            }
        }

        protected void ShowAvailable(object sender, EventArgs e)
        {
            try
            {
                showingAvailableByField = false;
                showingReserved = false;
                ViewState["showingReserved"] = showingReserved;
                ViewState["showingAvailableByField"] = showingAvailableByField;
                RunQuery(sender, e);
            }
            catch (Exception ex)
            {
                Message0.Text = Message0.Text + ". Exception " + ex.Message;
            }
        }

        protected void RunQuery(object sender, EventArgs e)
        {
            Message2.Text = "";
            if (loggedIn && teamName != null)
            {
                if (showingReserved)
                {
                    DataGrid1.Columns[0].HeaderText = "Release";
                    ViewState["showingReserved"] = true;
                    sqlStrBase = "exec usp_DisplaySlotsEx '" + Sql.Sqlify(teamName) + "',1,'00/00/00'," + "''";
                    sqlStrSorted = sqlStrBase + ",Date";
                    ViewState["sqlStrBase"] = sqlStrBase;
                }
                else
                {
                    DataGrid1.Columns[0].HeaderText = "Reserve";
                    ViewState["showingReserved"] = false;
                    if (showingAvailableByField)
                    {
                        ViewState["showingAvailableByField"] = true;
                        sqlStrBase = "exec usp_DisplaySlotsEx '" + Sql.Sqlify(teamNameForAvailableSlots) + "',2,'" +
                                     monthMenu.SelectedItem.Value + "/" + dayMenu.SelectedItem.Value + "/" + sCurYear +
                                     "','" +
                                     fieldMenu.SelectedItem.Value + "'";
                        sqlStrSorted = sqlStrBase + ",Date";
                    }
                    else
                    {
                        sqlStrBase = "exec usp_DisplaySlotsEx '" + Sql.Sqlify(teamNameForAvailableSlots) + "',2,'" +
                                     monthMenu.SelectedItem.Value + "/" + dayMenu.SelectedItem.Value + "/" + sCurYear +
                                     "'," +
                                     "''";
                        sqlStrSorted = sqlStrBase + ",Date";
                    }

                    ViewState["sqlStrBase"] = sqlStrBase;
                }

                DataGrid1.EditItemIndex = -1;
            }
#if no // nothing to do if not logged in
            else
            {
                if (showingReserved && !teamName.Contains("--"))
                {
                    DataGrid1.Columns[0].HeaderText = "";
                    sqlStrBase = "exec usp_DisplaySlotsEx '" + Sql.Sqlify(teamName) + "',1,'00/00/00'," + "''";
                    sqlStrSorted = sqlStrBase + ",Date";
                    ViewState["sqlStrBase"] = sqlStrBase;
                }
                else
                {
                    //  show available slots for a given day
                    DataGrid1.Columns[0].HeaderText = "";
                    if (showingAvailableByField)
                    {
                        sqlStrBase = "exec usp_DisplaySlotsEx 'ShowAll',0,'" + monthMenu.SelectedItem.Value + "/" +
                                     dayMenu.SelectedItem.Value + "/" + sCurYear + "','" +
                                     Sql.Sqlify(fieldMenu.SelectedItem.Value) + "'";
                        sqlStrSorted = sqlStrBase + ",Date";
                    }
                    else
                    {
                        sqlStrBase = "exec usp_DisplaySlotsEx 'ShowAll',0,'" + monthMenu.SelectedItem.Value + "/" +
                                     dayMenu.SelectedItem.Value + "/" + sCurYear + "'," + "''";
                        sqlStrSorted = sqlStrBase + ",Date";
                    }

                    ViewState["sqlStrBase"] = sqlStrBase;
                }

                Message2.ForeColor = System.Drawing.Color.Green;
                Message2.Text = "You must login to reserve fields.";
            }
#endif
            BindGrid();
        }

        protected void SortCommand(object sender, DataGridSortCommandEventArgs e)
        {
            sqlStrSorted = sqlStrBase + "," + e.SortExpression;
            BindGrid();
        }

        protected void DataGrid_Command(object sender, DataGridCommandEventArgs e)
        {
            if (((LinkButton) e.CommandSource).CommandName == "Delete")
                DeleteItem(e);
        }

        protected void DataGrid_Cancel(object sender, DataGridCommandEventArgs e) { }

        protected void DataGrid_Edit(object sender, DataGridCommandEventArgs e)
        {
            string SQLcmd;
            int temp;

            DataGrid1.EditItemIndex = e.Item.ItemIndex;
            if (DataGrid1.Columns[0].HeaderText == "Release")
            {
                DBConn.Open();
                SQLcmd = "exec usp_UpdateSlots 'Rel', '" + Sql.Sqlify(teamName) + "','" +
                         Sql.Sqlify(e.Item.Cells[2].Text) + "'";
                cmdMbrs = DBConn.CreateCommand();
                cmdMbrs.CommandText = SQLcmd;
                rdrMbrs = cmdMbrs.ExecuteReader();
                temp = -1;
                while (rdrMbrs.Read())
                    temp = rdrMbrs.GetInt32(0);

                if (temp == 0)
                {
                    Message2.Text = "Release successful";
                    Message2.ForeColor = System.Drawing.Color.Green;
                }

                if (temp == -1)
                {
                    Message2.Text = "Error releasing field";
                    Message2.ForeColor = System.Drawing.Color.Red;
                }

                DataGrid1.EditItemIndex = -1;
                rdrMbrs.Close();
                cmdMbrs.Dispose();
                DBConn.Close();
            }

            if (DataGrid1.Columns[0].HeaderText == "Reserve")
            {
                DBConn.Open();
                if (loggedInAsAdmin)
                    SQLcmd = "exec usp_UpdateSlots 'ResAdmin'";
                else
                    SQLcmd = "exec usp_UpdateSlots 'Res'";

                SQLcmd = SQLcmd + ", '" + Sql.Sqlify(teamName) + "','" + Sql.Sqlify(e.Item.Cells[2].Text) + "'";
                cmdMbrs = DBConn.CreateCommand();
                cmdMbrs.CommandText = SQLcmd;
                rdrMbrs = cmdMbrs.ExecuteReader();
                temp = -3;
                if (rdrMbrs.Read())
                    temp = rdrMbrs.GetInt32(0);

                if (temp == 0)
                {
                    Message2.Text = "Reservation successful";
                    Message2.ForeColor = System.Drawing.Color.Green;
                }
                else if (temp == -1)
                {
                    Message2.Text = "Field already reserved";
                    Message2.ForeColor = System.Drawing.Color.Red;
                }
                else if (temp == -2)
                {
                    Message2.Text = "Only two fields can be reserved per day";
                    Message2.ForeColor = System.Drawing.Color.Red;
                }
                else if (temp == -3)
                {
                    Message2.Text = "Error reserving field";
                    Message2.ForeColor = System.Drawing.Color.Red;
                }
                else if (temp == -4)
                {
                    Message2.Text = "Only one H5*/H6* can be reserved per week";
                    Message2.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    Message2.Text = "Unknown error reserving field (" + temp + ")";
                    Message2.ForeColor = System.Drawing.Color.Red;
                }

                DataGrid1.EditItemIndex = -1;
                // return to list of reserved fields
                DataGrid1.Columns[0].HeaderText = "Release";
                sqlStrBase = "exec usp_DisplaySlotsEx '" + Sql.Sqlify(teamName) + "',1,'00/00/00'," + "''";
                sqlStrSorted = sqlStrBase + ",Date";
                ViewState["sqlStrBase"] = sqlStrBase;
                rdrMbrs.Close();
                cmdMbrs.Dispose();
                DBConn.Close();
            }

            BindGrid();
        }

        protected void DeleteItem(DataGridCommandEventArgs e) { }

        protected void Item_Bound(object sender, DataGridItemEventArgs e)
        {
            LinkButton link;
            string strDateField;
            DateTime dateField;
            bool IsEnabled;
            bool IsloggedIn;

            IsloggedIn = (bool)(ViewState["loggedIn"] ?? false);

            if (e.Item.Cells[0].Controls.Count > 0)
            {
                link = (LinkButton) e.Item.Cells[0].Controls[0];
                strDateField = e.Item.Cells[3].Text;
                dateField = DateTime.Parse(strDateField);
                if (e.Item.Cells[1].Text.Length == 0 || e.Item.Cells[1].Text == "&nbsp;")
                    IsEnabled = false;
                else
                {
                    IsEnabled = true;
                    link.ToolTip = e.Item.Cells[1].Text;
                }

                if (!IsloggedIn)
                {
                    link.Enabled = false;
                    link.ToolTip = "Not logged in";
                }

                DateTime dttmNow;

                if (link != null
                    && DateTime.Compare(dateField, DateTime.UtcNow.AddHours(-8).Date) <= 0
                    && (bool) ViewState["showingReserved"])
                {
                    link.Enabled = false;
                    link.ToolTip = "date has passed: " + dateField + " < " + DateTime.UtcNow.AddHours(-8).Date;
                }

                if (link != null
                    && IsEnabled && !(bool) ViewState["showingReserved"])
                {
                    link.Enabled = false;
                }
            }
        }

        protected void ShowAvailableByField(object sender, EventArgs e)
        {
            try
            {
                showingReserved = false;
                showingAvailableByField = true;
                ViewState["showingReserved"] = showingReserved;
                ViewState["showingAvailableByField"] = showingAvailableByField;
                RunQuery(sender, e);
            }
            catch (Exception ex)
            {
                Message0.Text = Message0.Text + ", Exception: " + ex.Message;
            }
        }
    }
}