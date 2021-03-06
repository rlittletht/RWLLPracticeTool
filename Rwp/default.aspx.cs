﻿using System;
using Microsoft.Owin;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.EnterpriseServices;
using System.Linq;
using System.Security.Cryptography;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Web;
using System.Windows.Forms;
using System.Xml.Serialization;
using Microsoft.Owin.Security.Notifications;
using Owin;
using TCore;

namespace Rwp
{
    public partial class default1 : System.Web.UI.Page
    {
        private Auth m_auth;
#if PRODHOST
        static string s_sRoot = "";
#else
        static string s_sRoot = "/rwp";
#endif
        private SqlConnection DBConn;
        private string sqlStrSorted;
        private SqlCommand cmdMbrs;
        private SqlDataReader rdrMbrs;

        public default1()
        {
        }

        void FillInCalendarLink()
        {
            string s;

            s = teamName.Replace(" ", "%20");

            txtCalendarFeedLink.Text = $"http://rwllpractice.azurewebsites.net/icsfeed.aspx?Team={s}";
        }

        #region Persisted ViewState

        private string SqlBase
        {
            get { return TGetState<string>("sqlStrBase", null); }
            set { SetState("sqlStrBase", value); }
        }

        private string teamName
        {
            get { return GetTeamName(); }
        }
        private string teamNameForAvailableSlots;
        
        // team name used to query for reserved and available slots
        private bool ShowingReserved
        {
            get { return TGetState("showingReserved", true); }
            set { SetState("showingReserved", value); }
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

        #endregion

        private bool IsLoggedIn => m_auth.IsLoggedIn;
        private bool LoggedInAsAdmin => m_auth.CurrentPrivs.privs == Auth.UserPrivs.AdminPrivs;

        private string timeZoneId => m_auth.CurrentPrivs.sTimezone;

        private string sCurYear;

        protected void Page_Load(object sender, EventArgs e)
        {
            m_auth = new Auth(LoginOutButton, Request, Session, Context.GetOwinContext().Environment["System.Web.HttpContextBase"] as HttpContextBase, ViewState, $"{s_sRoot}/default.aspx", null, null, OnBeforeSignout, null);

            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings["dbSchedule"];
            string sSqlConnectionString = conn.ConnectionString;

            DBConn = new SqlConnection(sSqlConnectionString);

            sCurYear = DateTime.UtcNow.Year.ToString();

            Message0.Text =
                $"Redmond West Little League Practice Scheduler v2.alpha (Server DateTime = {DateTime.UtcNow.AddHours(-8)} ({sCurYear})";

            try
            {
                LoadPrivsAndSetupPage();
            }
            catch (Exception exc)
            {
                Message0.Text = exc.Message;
            }

            if (timeZoneInfo == null && timeZoneId != null)
                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            m_auth.SetupLoginLogout();
        }

        private TimeZoneInfo timeZoneInfo;

        void LoadPrivsAndSetupPage()
        {
            teamNameForAvailableSlots = teamName;

            if (!String.IsNullOrEmpty(SqlBase))
                sqlStrSorted = SqlBase + ",SlotStart";

            if (!IsPostBack)
            {
                divCalendarFeedLink.Visible = ShowingCalLink;

                Auth.UserData data = LoadPrivs();

                if (timeZoneInfo == null && data.sTimezone != null)
                    timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(data.sTimezone);

                if (!IsLoggedIn)
                    SetLoggedOff();

                ShowHideAdmin(data);
                BindFieldDropdown();
            }

            FillInCalendarLink();
        }

        #region Auth/Privs

        void ShowHideAdmin(Auth.UserData data)
        {
            if (!LoggedInAsAdmin)
            {
                divAdminFunctions.Visible = false;
                return;
            }

            divAdminFunctions.Visible = true;
        }

        protected void OnBeforeSignout(object sender, EventArgs e)
        {
            Message1.Text = "";
            SetLoggedOff();
            RunQuery(sender, e);
        }

        void SetLoggedOff()
        {
            m_auth.SetLoggedOff();

            SqlBase = "";
        }

        void FillTeamList(Auth.UserData data)
        {
            int i = 0;
            int iChecked = -1;

            teamMenu.Items.Clear();
            if (data.plsTeams == null)
                return;

            foreach (string sTeam in data.plsTeams)
            {
                teamMenu.Items.Add(sTeam);
                if (data.sTeamName == sTeam)
                    iChecked = i;
                i++;
            }

            if (iChecked != -1)
                teamMenu.SelectedIndex = iChecked;
        }

        Auth.UserData LoadPrivs()
        {
            if (!m_auth.IsAuthenticated())
                return Auth.EmptyAuth();

            Auth.UserData userData;
                
            m_auth.LoadPrivs(DBConn);

            userData = m_auth.CurrentPrivs;
            if (userData.sTimezone != null)
                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(userData.sTimezone);

            FillTeamList(userData);

            if (m_auth.IsLoggedIn)
            {
                Message1.Text = $"Welcome to RedmondWest Practice Tool ({m_auth.Identity()})";

                Message1.ForeColor = System.Drawing.Color.Green;
                Message2.Text = "";
                DataGrid1.Columns[0].HeaderText = "Release";
                SqlBase = "exec usp_DisplaySlotsEx '"
                          + Sql.Sqlify(teamName)
                          + "',1,'"
                          + sCurYear
                          + "-01-01',"
                          + "'', '"
                          + GetWindowStartForQuery(60 * 12)
                          + "'";
                if (!String.IsNullOrEmpty(SqlBase))
                    sqlStrSorted = SqlBase + ",SlotStart";
                ShowHideAdmin(userData);
                BindGrid();
            }
            else
            {
                SetLoggedOff();
                Message1.Text = $"User '{m_auth.Tenant()}\\{m_auth.Identity()} not authorized! If you believe this is incorrect, please copy this entire message and sent it to your administrator.";
                Message1.ForeColor = System.Drawing.Color.Red;
            }

            return userData;
        }
        #endregion

        #region Bindings
        void BindFieldDropdown()
        {
            DBConn.Open();
            // populate the fieldMenu
            string sql= "exec usp_PopulateFieldList";
            cmdMbrs = DBConn.CreateCommand();

            cmdMbrs.CommandText = sql;
            rdrMbrs = cmdMbrs.ExecuteReader();
            fieldMenu.DataSource = rdrMbrs;
            fieldMenu.DataTextField = "Field";
            fieldMenu.DataValueField = "Field";
            fieldMenu.DataBind();
            rdrMbrs.Close();
            rdrMbrs.Dispose();
            cmdMbrs.Dispose();
            DBConn.Close();
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
                rdrMbrs.Dispose();
                cmdMbrs.Dispose();
                DBConn.Close();
            }
        }
        #endregion

        string GetTeamName()
        {
            return teamMenu.SelectedValue;
        }

        #region Commands
        protected void ShowICalFeedLink(object sender, EventArgs e)
        {
            Response.Redirect($"{Startup.s_sFullRoot}/CalendarLink.aspx");
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
        #endregion

        #region Query/Data

        /*----------------------------------------------------------------------------
        	%%Function: GetWindowStartForQuery
        	%%Qualified: Rwp.default1.GetWindowStartForQuery
        	
            Take the current date/time and turn it into the start of the reset window.

            (If this was just midnight, then truncate the hour from the datetime, and
            that's your reset window. Not so simple when it resets at a different time

            minutesOffsetForReset is the number of minutes into the day that is the
            reset point. (midnight = 0 minutes. noon = 12 * 60 minutes)
        ----------------------------------------------------------------------------*/
        private DateTime GetWindowStartForQuery(long minutesOffsetForReset)
        {
            DateTime dttmCur = DateTime.UtcNow;

            // convert this to local time according to the users timezone
            dttmCur = TimeZoneInfo.ConvertTimeFromUtc(dttmCur, timeZoneInfo);

            DateTime dttmReset = dttmCur.Date;
            dttmReset = dttmReset.AddMinutes(minutesOffsetForReset);

            // if we are beyond today's reset, then yesterday is the correct reset
            if (dttmReset > dttmCur)
                dttmReset = dttmReset.AddDays(-1);

            // and now return in UTC
            return TimeZoneInfo.ConvertTimeToUtc(dttmReset, timeZoneInfo);
        }

        protected string LocalDateTimeFromObject(object o)
        {
            if (o is System.DBNull)
                return "";

            DateTime? dttmUTC = (DateTime?) o;

            if (dttmUTC == null)
                return "";

            return TimeZoneInfo.ConvertTimeFromUtc(dttmUTC.Value, timeZoneInfo).ToString("g");
        }

        protected string LocalDateFromUtc(DateTime dttmUTC)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dttmUTC, timeZoneInfo).ToShortDateString();
        }

        protected string WeekdayFromUtc(DateTime dttmUTC)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dttmUTC, timeZoneInfo).ToString("ddd");
        }

        protected string SlotStartTimeFromUtc(DateTime dttmUTC)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dttmUTC, timeZoneInfo).ToString("hh:mm tt");
        }

        protected string SlotEndTimeFromUtcLength(DateTime dttmUTC, int minutes)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dttmUTC.AddMinutes(minutes), timeZoneInfo).ToString("hh:mm tt");
        }

        protected string SlotLengthFormatMinutes(int minutes)
        {
            TimeSpan ts = TimeSpan.FromMinutes(minutes);
            return ts.ToString("hh\\:mm");
        }

        private string GetDateStringForQuery()
        {
            string sDateShort = monthMenu.SelectedItem.Value + "/" + dayMenu.SelectedItem.Value + "/" + sCurYear;

            DateTime dttm = DateTime.Parse(sDateShort);
            return TimeZoneInfo.ConvertTimeToUtc(dttm, timeZoneInfo).ToString();
        }

        protected void RunQuery(object sender, EventArgs e)
        {
            Message2.Text = "";
            if (IsLoggedIn && teamName != null)
            {
                if (ShowingReserved)
                {
                    DataGrid1.Columns[0].HeaderText = "Release";
                    SqlBase = "exec usp_DisplaySlotsEx '"
                              + Sql.Sqlify(showAllReserved.Checked ? "ShowAll" : teamName)
                              + "',1,'00/00/00',"
                              + "'', '"
                              + GetWindowStartForQuery(60 * 12)
                              + "'";
                    sqlStrSorted = SqlBase + ",SlotStart";
                }
                else
                {
                    DataGrid1.Columns[0].HeaderText = "Reserve";
                    if (ShowingAvailableByField)
                    {
                        SqlBase = "exec usp_DisplaySlotsEx '"
                                  + Sql.Sqlify(teamNameForAvailableSlots)
                                  + "',2,'"
                                  + GetDateStringForQuery()
                                  + "','"
                                  + fieldMenu.SelectedItem.Value
                                  + "', '"
                                  + GetWindowStartForQuery(60 * 12)
                                  + "'";
                        sqlStrSorted = SqlBase + ",SlotStart";
                    }
                    else
                    {
                        SqlBase = "exec usp_DisplaySlotsEx '"
                                  + Sql.Sqlify(teamNameForAvailableSlots)
                                  + "',2,'"
                                  + GetDateStringForQuery()
                                  + "',"
                                  + "'', '"
                                  + GetWindowStartForQuery(60 * 12)
                                  + "'";
                        sqlStrSorted = SqlBase + ",SlotStart";
                    }
                }

                DataGrid1.EditItemIndex = -1;
            }
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
                rdrMbrs.Dispose();
                cmdMbrs.Dispose();
                DBConn.Close();
            }

            if (DataGrid1.Columns[0].HeaderText == "Reserve")
            {
                DBConn.Open();
                if (LoggedInAsAdmin && teamName == "Administrator")
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
                SqlBase = "exec usp_DisplaySlotsEx '"
                          + Sql.Sqlify(teamName)
                          + "',1,'00/00/00',"
                          + "'', '"
                          + GetWindowStartForQuery(60 * 12)
                          + "'";
                sqlStrSorted = SqlBase + ",SlotStart";
                rdrMbrs.Close();
                rdrMbrs.Dispose();
                cmdMbrs.Dispose();
                DBConn.Close();
            }

            BindGrid();
        }

        protected void DeleteItem(DataGridCommandEventArgs e) { }

        protected void Item_Bound(object sender, DataGridItemEventArgs e)
        {
            LinkButton link;
            bool IsEnabled;

            // do transformations here (like converting our slotstart to local time
//            e
//            e.Item.Cells[3].Text = LocalDateFromUtc(e.Item.Cells[3].DataB
            if (e.Item.ItemType == ListItemType.Item
                || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                if (e.Item.Cells[0].Controls.Count > 0)
                {
                    string strDateField;
                    DateTime dateField;

                    link = (LinkButton) e.Item.Cells[0].Controls[0];

                    // at this point, Item.Cells[3] is the UTC slotStart
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

                    if (link != null
                        && DateTime.Compare(dateField, DateTime.UtcNow.Date) <= 0
                        && (bool) ShowingReserved)
                    {
                        link.Enabled = false;
                        link.ToolTip = "date has passed: " + 
                                       TimeZoneInfo.ConvertTimeFromUtc(dateField, timeZoneInfo) 
                                       + " < " 
                                       + TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo).Date;
                    }

                    if (link != null
                        && IsEnabled
                        && !(bool) ShowingReserved)
                    {
                        link.Enabled = false;
                    }

                    if (ShowingReserved && showAllReserved.Checked)
                    {
                        link.Enabled = false;
                        link.ToolTip = "Can't release in ShowAll";
                    }
                }

                // finally, fixup the string to be local time
                e.Item.Cells[3].Text = 
                    TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(e.Item.Cells[3].Text), timeZoneInfo).ToShortDateString();
            }
        }
        #endregion

        protected void OnTeamMenuItemChanged(object sender, EventArgs e)
        {
            // they have selected a new teamname to work as, load the privs for that
            m_auth.LoadPrivs(DBConn, teamName);
            LoadPrivsAndSetupPage();
        }
    }
}