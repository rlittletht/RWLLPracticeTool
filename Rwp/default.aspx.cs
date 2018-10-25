using System;
using Microsoft.Owin;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
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

            s = lblTeamName.Text.Replace(" ", "%20");

            txtCalendarFeedLink.Text = $"http://rwllpractice.azurewebsites.net/icsfeed.aspx?Team={s}";
        }

        private SqlConnection DBConn;
        private string sqlStrSorted;

        private string SqlBase
        {
            get { return TGetState<string>("sqlStrBase", null); }
            set { SetState("sqlStrBase", value); }
        }


        private SqlConnection conClsf;
        private SqlCommand cmdMbrs;
        private SqlDataReader rdrMbrs;

        private string teamName;
        private string teamNameForAvailableSlots;
        
        // team name used to query for reserved and available slots
        private bool ShowingReserved
        {
            get { return TGetState("showingReserved", true); }
            set { SetState("showingReserved", value); }
        }

        private bool IsLoggedIn
        {
            get { return TGetState("isLoggedIn", false); }
            set { SetState("isLoggedIn", value); }
        }

        private bool ShowingAvailableByField
        {
            get { return TGetState("showingAvailableByField", false); }
            set { SetState("showingAvailableByField", value); }
        }

        private bool ShowingCalLink
        {
            get { return TGetState("showingCalLink", false); }
            set { SetState("showingCalLink", value); }
        }

        private bool LoggedInAsAdmin
        {
            get { return TGetState("loggedInAsAdmin", false); }
            set { SetState("loggedInAsAdmin", value); }
        }

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

        T TGetState<T>(string sState, T tDefault)
        {
            T tValue = tDefault;

            if (ViewState[sState] == null)
                ViewState[sState] = tValue;
            else
                tValue = (T)ViewState[sState];

            return tValue;
        }

        void SetState<T>(string sState, T tValue)
        {
            ViewState[sState] = tValue;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings["dbSchedule"];
            string sSqlConnectionString = conn.ConnectionString;

            DBConn = new SqlConnection(sSqlConnectionString);

            sCurYear = DateTime.UtcNow.Year.ToString();
            string sIdentity = System.Security.Claims.ClaimsPrincipal.Current.FindFirst("preferred_username")?.Value;

            Message0.Text =
                $"Redmond West Little League Practice Scheduler v1.9 (Server DateTime = {DateTime.UtcNow.AddHours(-8)} ({sCurYear})";

            try
            {
                teamName = lblTeamName.Text;
                teamNameForAvailableSlots = teamName;

                // this teams reservations
                //sqlStrBase = "exec usp_DisplaySlotsEx '" + Sqlify(teamName) + "',1,'00 / 00 / 00'"
                //			DataGrid1.Columns(0).HeaderText = "Release"

                if (!String.IsNullOrEmpty(SqlBase))
                    sqlStrSorted = SqlBase + ",Date";

                divCalendarFeedLink.Visible = ShowingCalLink;

                if (LoggedInAsAdmin)
                    teamNameForAvailableSlots = "Administrator";

                if (sIdentity != null)
                    LoadPrivs(sIdentity);

                if (!IsLoggedIn)
                    SetLoggedOff();

                if (!IsPostBack)
                {
                    BindTeamDropdown();
                    BindFieldDropdown();
                }

                FillInCalendarLink();
            }
            catch (Exception exc)
            {
                Message0.Text = exc.Message;
            }

            SetupLoginLogout();
        }

        void BindFieldDropdown()
        {
            DBConn.Open();
            // populate the fieldMenu
            sqlStrSorted = "exec usp_PopulateFieldList";
            cmdMbrs = DBConn.CreateCommand();

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

        void BindTeamDropdown()
        {
        }
        protected void BindGrid()
        {
            if (String.IsNullOrEmpty(sqlStrSorted))
            {
                DataGrid1.DataSource = null;
                DataGrid1.DataBind();
            }
            else
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
        }

        void SetLoggedOff()
        {
            IsLoggedIn = false;
            LoggedInAsAdmin = false;
            SqlBase = "";
        }


        void LoadPrivs(string sIdentity)
        {
            string sqlStrLogin;
            int temp;

            SqlBase = "";

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
                IsLoggedIn = true;
                lblTeamName.Text = teamName;
                Message1.ForeColor = System.Drawing.Color.Green;
                Message2.Text = "";
                DataGrid1.Columns[0].HeaderText = "Release";
                SqlBase = "exec usp_DisplaySlotsEx '" + Sql.Sqlify(teamName) + "',1,'" + sCurYear + "-01-01'," +
                             "''";
                if (!String.IsNullOrEmpty(SqlBase))
                    sqlStrSorted = SqlBase + ",Date";
                BindGrid();
            }
            else
            {
                SetLoggedOff();
                Message1.Text = $"User '{sIdentity} not authorized! If you believe this is incorrect, please copy this entire message and sent it to your administrator.";
                Message1.ForeColor = System.Drawing.Color.Red;
            }

        }

        void SetupLoginLogout()
        {
            if (Request.IsAuthenticated)
            {
                LoginOutButton.Click -= ValidateLogin;
                LoginOutButton.Click += LogOff;
                LoginOutButton.ImageUrl = "signout.png";
            }
            else
            {
                LoginOutButton.Click -= LogOff;
                LoginOutButton.Click += ValidateLogin;
                LoginOutButton.ImageUrl = "signin.png";
            }
        }


        protected void LogOff(object sender, EventArgs e)
        {
            Message1.Text = "";
            SetLoggedOff();
            RunQuery(sender, e);
            SignOut();
        }


        protected void ValidateLogin(object sender, EventArgs e)
        {
            SignIn();
        }

        protected void ShowICalFeedLink(object sender, EventArgs e)
        {
            divCalendarFeedLink.Visible = true;
            ShowingCalLink = true;
            // RunQuery(sender, e)
        }

        protected void HideCalendarFeedLink(object sender, EventArgs e)
        {
            divCalendarFeedLink.Visible = false;
            ShowingCalLink = false;
            // RunQuery(sender, e)
        }

        protected void ShowReserved(object sender, EventArgs e)
        {
            try
            {
                ShowingReserved = true;
                ShowingAvailableByField = false;
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
                ShowingAvailableByField = false;
                ShowingReserved = false;
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
            if (IsLoggedIn && teamName != null)
            {
                if (ShowingReserved)
                {
                    DataGrid1.Columns[0].HeaderText = "Release";
                    SqlBase = "exec usp_DisplaySlotsEx '" + Sql.Sqlify(teamName) + "',1,'00/00/00'," + "''";
                    sqlStrSorted = SqlBase + ",Date";
                }
                else
                {
                    DataGrid1.Columns[0].HeaderText = "Reserve";
                    if (ShowingAvailableByField)
                    {
                        SqlBase = "exec usp_DisplaySlotsEx '" + Sql.Sqlify(teamNameForAvailableSlots) + "',2,'" +
                                     monthMenu.SelectedItem.Value + "/" + dayMenu.SelectedItem.Value + "/" + sCurYear +
                                     "','" +
                                     fieldMenu.SelectedItem.Value + "'";
                        sqlStrSorted = SqlBase + ",Date";
                    }
                    else
                    {
                        SqlBase = "exec usp_DisplaySlotsEx '" + Sql.Sqlify(teamNameForAvailableSlots) + "',2,'" +
                                     monthMenu.SelectedItem.Value + "/" + dayMenu.SelectedItem.Value + "/" + sCurYear +
                                     "'," +
                                     "''";
                        sqlStrSorted = SqlBase + ",Date";
                    }
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
            sqlStrSorted = SqlBase + "," + e.SortExpression;
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
                if (LoggedInAsAdmin)
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
                SqlBase = "exec usp_DisplaySlotsEx '" + Sql.Sqlify(teamName) + "',1,'00/00/00'," + "''";
                sqlStrSorted = SqlBase + ",Date";
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

                if (!IsLoggedIn)
                {
                    link.Enabled = false;
                    link.ToolTip = "Not logged in";
                }

                DateTime dttmNow;

                if (link != null
                    && DateTime.Compare(dateField, DateTime.UtcNow.AddHours(-8).Date) <= 0
                    && (bool) ShowingReserved)
                {
                    link.Enabled = false;
                    link.ToolTip = "date has passed: " + dateField + " < " + DateTime.UtcNow.AddHours(-8).Date;
                }

                if (link != null
                    && IsEnabled && !(bool) ShowingReserved)
                {
                    link.Enabled = false;
                }
            }
        }

        protected void ShowAvailableByField(object sender, EventArgs e)
        {
            try
            {
                ShowingReserved = false;
                ShowingAvailableByField = true;
                RunQuery(sender, e);
            }
            catch (Exception ex)
            {
                Message0.Text = Message0.Text + ", Exception: " + ex.Message;
            }
        }
    }
}