using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TCore;

namespace Rwp
{
    public partial class CalendarLinkPage : System.Web.UI.Page
    {
#if PRODHOST
        static string s_sRoot = "";
        static string s_sFullRoot = "https://rwllpractice.azurewebsites.net";
#elif STAGEHOST
        static string s_sRoot = "/rwp";
        private static string s_sFullRoot = "https://thetasoft2.azurewebsites.net/rwp";
#else
        static string s_sRoot = "/rwp";
        static string s_sFullRoot = "http://localhost/rwp";
#endif
        private Auth m_auth;
        private ApiInterop m_apiInterop;
        private Auth.UserData m_userData;
        private SqlConnection DBConn;

        #region Persisted ViewState
        T TGetState<T>(string sState, T tDefault)
        {
            T tValue = tDefault;

            if (ViewState[sState] == null)
                ViewState[sState] = tValue;
            else
                tValue = (T)ViewState[sState];

            return tValue;
        }

        private string SqlQuery
        {
            get { return TGetState<string>("sqlStrQuery", null); }
            set { SetState("sqlStrQuery", value); }
        }

        void SetState<T>(string sState, T tValue)
        {
            ViewState[sState] = tValue;
        }
        #endregion


        protected void Page_Load(object sender, EventArgs e)
        {
            m_auth = new Auth(LoginOutButton, Request, Session,
                Context.GetOwinContext().Environment["System.Web.HttpContextBase"] as HttpContextBase, ViewState,
                $"{s_sRoot}/CalendarLink.aspx", null, OnAfterLogin, null, null);
            m_apiInterop = new ApiInterop(Context, Server, Startup.apiRoot);

            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings["dbSchedule"];
            string sSqlConnectionString = conn.ConnectionString;

            DBConn = new SqlConnection(sSqlConnectionString);

            m_userData = m_auth.CurrentPrivs; // if they login, then we will load new privs

            m_auth.SetupLoginLogout();
            DataGrid1.ItemCommand += new DataGridCommandEventHandler(DataGrid_Command);
            if (!IsPostBack)
            {
                if (m_auth.IsAuthenticated() && m_userData.privs == Auth.UserPrivs.NotAuthenticated)
                    m_userData = m_auth.LoadPrivs(DBConn);

                FillTeamList(m_userData);

                if (!string.IsNullOrEmpty(m_userData.sTeamName))
                {
                    BuildPageSqlQuery();
                    BindSource();
                }
                else
                {
                    SqlQuery = null;
                    BindSource();
                }
            }
        }

        void FillTeamList(Auth.UserData data)
        {
            int i = 0;
            int iChecked = -1;

            teamMenu.Items.Clear();
            if (data.plsTeams == null)
            {
                teamMenu.Items.Add(new ListItem("[No Team Selected]", null));
                return;
            }

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

        protected void OnTeamMenuItemChanged(object sender, EventArgs e)
        {
            m_userData = m_auth.LoadPrivs(DBConn, teamMenu.SelectedValue);
            BuildPageSqlQuery();
            BindSource();
        }

        void OnAfterLogin(object sender, EventArgs e)
        {
            m_userData = m_auth.LoadPrivs(DBConn);
            FillTeamList(m_userData);
            BindSource();
        }

        private void ReportSr(RSR sr, string sOperation)
        {
            if (!sr.Result)
            {
                divResults.Visible = true;
                divResults.InnerText = sr.Reason;
            }
            else
            {
                divResults.InnerText = String.Format("{0} returned no errors.", sOperation);
            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoCreateLink
        	%%Qualified: Rwp.CalendarLinkPage.DoCreateLink
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        protected void DoCreateLink(object sender, EventArgs e)
        {
            RSR sr;

            if (teamMenu.SelectedValue == null)
            {
                sr = new RSR() {Reason = "Cannot create a link with no team selected"};
                ReportSr(sr, "CreateLink");
                return;
            }

            string sComment = txtComment.Text;
            string sAuthority = m_auth.Identity();
            string sTeam = m_auth.CurrentPrivs.sTeamName;
            Guid guidLinkID = System.Guid.NewGuid();
            CalendarLink link = new CalendarLink()
            {
                Authority = sAuthority, Comment = txtComment.Text, CreateDate = DateTime.Now, Link = guidLinkID, Team = teamMenu.SelectedValue
            };

            sr = m_apiInterop.CallServicePost<RSR, CalendarLink>("api/calendar/PostCalendarLink", link, true);

            ReportSr(sr, "CreateLink");
            txtCalendarFeedLink.Text = GetIcsLinkAddress(link.Link.ToString());

            BindSource(); // force a refresh
        }

        string GetIcsLinkAddress(string LinkID)
        {
            return $"{s_sFullRoot}/icsfeed.aspx?linkID={LinkID.ToString()}";
        }

        #region Query/Data Binding
        /*----------------------------------------------------------------------------
        	%%Function: BuildSqlQuery
        	%%Qualified: Rwp.CalendarLinkPage.BuildSqlQuery
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        string BuildSqlQuery(string sTeamID, string sSort)
        {
            string sWhere;
            string sOrderBy;

            if (!string.IsNullOrEmpty(sTeamID) && m_userData.privs != Auth.UserPrivs.AdminPrivs)
                sWhere = $"where TeamID = '{Sql.Sqlify(sTeamID)}'";
            else
                sWhere = "";

            if (!string.IsNullOrEmpty(sSort))
                sOrderBy = $"order by {Sql.Sqlify(sSort)}";
            else
                sOrderBy = "";

            return $"select LinkID, TeamID, Authority, CreateDate, Comment from rwllcalendarlinks {sWhere} {sOrderBy}";
        }
        
        void BuildPageSqlQuery()
        {
            SqlQuery = BuildSqlQuery(teamMenu.SelectedValue, null);
        }

        private SqlCommand cmdMbrs;
        private SqlDataReader rdrMbrs;
        
        /*----------------------------------------------------------------------------
        	%%Function: BindSource
        	%%Qualified: Rwp.CalendarLinkPage.BindSource
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        void BindSource()
        {
            if (String.IsNullOrEmpty(SqlQuery))
            {
                DataGrid1.DataSource = null;
                DataGrid1.DataBind();
            }
            else
            {
                DBConn.Open();
                cmdMbrs = DBConn.CreateCommand();
                cmdMbrs.CommandText = SqlQuery;
                rdrMbrs = cmdMbrs.ExecuteReader();
                DataGrid1.DataSource = rdrMbrs;
                DataGrid1.DataBind();

                DataGrid1.Visible = DataGrid1.Items.Count != 0;
                rdrMbrs.Close();
                cmdMbrs.Dispose();
                DBConn.Close();
            }
        }


        void DataGrid_Command(Object sender, DataGridCommandEventArgs e)
        {
            switch (((LinkButton) e.CommandSource).CommandName)
            {
                case "Delete":
                    DataGrid_DeleteItem(sender, e);
                    break;
                case "GetLink":
                    DataGrid_GetLink(sender, e);
                    break;
                default:
                    // Do nothing.
                    break;
            }
        }

        void DataGrid_GetLink(Object sender, DataGridCommandEventArgs e)
        {
            TableCell itemCell = e.Item.Cells[0];
            string linkID = itemCell.Text;

            txtCalendarFeedLink.Text = GetIcsLinkAddress(linkID);
        }

        /*----------------------------------------------------------------------------
        	%%Function: DataGrid_DeleteItem
        	%%Qualified: Rwp.CalendarLinkPage.DataGrid_DeleteItem
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        void DataGrid_DeleteItem(Object sender, DataGridCommandEventArgs e)
        {
            // e.Item is the table row where the command is raised. For bound
            // columns, the value is stored in the Text property of a TableCell.
            TableCell itemCell = e.Item.Cells[0];
            string item = itemCell.Text;

            // Remove the selected item from the data source
            RSR sr = m_apiInterop.CallService<RSR>($"api/calendar/RevokeCalendarLink/{item}", true);

            ReportSr(sr, "CreateLink");

            // Rebind the data source to refresh the DataGrid control.
            BindSource();
        }
        #endregion
    }
}