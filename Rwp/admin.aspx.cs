using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.Windows.Forms;
using TextBox = System.Web.UI.WebControls.TextBox;
using TCore;

namespace Rwp
{
    public partial class AdminPage : System.Web.UI.Page
    {
        #if PRODHOST
        static string s_sRoot = "";
        #else
        static string s_sRoot = "/rwp";
        #endif

        private Auth m_auth;
        private SqlConnection DBConn;
        private Auth.UserData m_userData;
        private ApiInterop m_apiInterop;

        static string ExtractServerNameFromConnection(string sConnection)
        {
            Regex rex = new Regex(".*server[ \t]*=[ \t]*([^;]*)[ \t]*;.*", RegexOptions.IgnoreCase);

            Match m = rex.Match(sConnection);

            return m.Groups[1].Value;
        }

        /*----------------------------------------------------------------------------
        	%%Function: CheckServiceServerConsistency
        	%%Qualified: Rwp.AdminPage.CheckServiceServerConsistency
        	%%Contact: rlittle
        	
            verify that the web client is consistent with the web api (the web client
            access the SQL server as well as the web api -- it sucks when they
            don't agree on the SQL server to be working with)
        ----------------------------------------------------------------------------*/
        void CheckServiceServerConsistency(string sSqlConnectionString)
        {
            if (!m_auth.IsLoggedIn)
                return;

            ServerInfo si;

            si = m_apiInterop.CallService<ServerInfo>("api/core/GetServerInfo", false);

            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings["dbSchedule"];

            string sSqlServer = ExtractServerNameFromConnection(sSqlConnectionString);
            if (String.Compare(si.sSqlServerHash, sSqlServer, true) != 0)
                throw new Exception($"SQL SERVER MISMATCH: {si.sSqlServerHash} != {sSqlServer}");
        }

        /*----------------------------------------------------------------------------
        	%%Function: Page_Load
        	%%Qualified: Rwp.AdminPage.Page_Load
        	%%Contact: rlittle
        	
            Initialize authentication and api interop
        ----------------------------------------------------------------------------*/
        protected void Page_Load(object sender, EventArgs e)
        {
            m_auth = new Auth(LoginOutButton, Request, Session, Context.GetOwinContext().Environment["System.Web.HttpContextBase"] as HttpContextBase, ViewState, $"{s_sRoot}/admin.aspx", null, null, null, null);
            m_apiInterop = new ApiInterop(Context, Server, Startup.apiRoot);

            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings["dbSchedule"];
            string sSqlConnectionString = conn.ConnectionString;

            DBConn = new SqlConnection(sSqlConnectionString);

            m_userData = m_auth.LoadPrivs(DBConn);
            ipClient.InnerText = Request.UserHostAddress;

            CheckServiceServerConsistency(sSqlConnectionString);
            EnableUIForAdmin();

            m_auth.SetupLoginLogout();
            GoHome.Click += DoGoHome;
            if (!IsPostBack)
            {
                rowAddUser.Visible = false;
                addNoticeTable.Visible = false;
                BindGrid();
            }
        }

        public void DoGoHome(object sender, ImageClickEventArgs args)
        {
            Response.Redirect(Startup.s_sFullRoot);
        }

        /*----------------------------------------------------------------------------
        	%%Function: ReportSr
        	%%Qualified: Rwp.AdminPage.ReportSr
        	%%Contact: rlittle
        	
            report the SR to the web page
        ----------------------------------------------------------------------------*/
        private void ReportSr(RSR sr, string sOperation)
        {
            if (!sr.Result)
                {
                divError.Visible = true;
                divError.InnerText = sr.Reason;
                }
            else
                {
                divError.InnerText = String.Format("{0} returned no errors.", sOperation);
                }
        }

        RSR CheckAdmin()
        {
            RSR sr = new RSR();

            if (m_userData.privs != Auth.UserPrivs.AdminPrivs)
            {
                sr.Result = false;
                sr.Reason = $"User does not have administrative privileges";

                return sr;
            }

            sr.Result = true;
            return sr;
        }

        void EnableUIForAdmin()
        {
            bool fAdmin = m_userData.privs == Auth.UserPrivs.AdminPrivs;

            btnDownloadTeams.Enabled = fAdmin;
            btnClearTeams.Enabled = fAdmin;
            btnUploadTeams.Enabled = fAdmin;
            btnDownloadSlots.Enabled = fAdmin;
            btnClearLastYear.Enabled = fAdmin;
            btnUploadSlots.Enabled = fAdmin;
            btnAddNotice.Enabled = fAdmin;
            btnAddUser.Enabled = fAdmin;
            
            //            btnClearAllSlots.Enabled = fAdmin;
            //            btnClearAuth.Enabled = fAdmin;
        }

        /* D O  D E L E T E  S L O T S */
        /*----------------------------------------------------------------------------
        	%%Function: DoDeleteSlots
        	%%Qualified: Rwp.AdminPage.DoDeleteSlots
        	%%Contact: rlittle

        ----------------------------------------------------------------------------*/
        protected void DoDeleteSlots(object sender, EventArgs e)
        {
            RSR sr = CheckAdmin();

            if (!sr.Result)
            {
                ReportSr(sr, "ipc");
                return;
            }

            try
            {
                sr = m_apiInterop.CallService<RSR>("api/slot/DeleteAllSlots", true);
            }
            catch (Exception exc)
            {
                sr = new RSR();
                sr.Result = false;
                sr.Reason = exc.Message;
            }

            ReportSr(sr, "Delete All Slots");
        }

		/*----------------------------------------------------------------------------
			%%Function: DoDelete2014Slots
			%%Qualified: Rwp.AdminPage.DoDelete2014Slots
			
            Intended to be changed every year, currently hardcoded to 2014 (will be
            2018 once the 2019 season starts)
		----------------------------------------------------------------------------*/
		protected void DoDelete2014Slots(object sender, EventArgs e)
		{
		    RSR sr = CheckAdmin();

		    if (!sr.Result)
		    {
		        ReportSr(sr, "ipc");
		        return;
		    }

		    sr = m_apiInterop.CallService<RSR>($"api/slot/DeleteSlotsByYear/2014", true);
			ReportSr(sr, "Delete 2014 Slots");
		}

		/*----------------------------------------------------------------------------
			%%Function: DoDeleteTeams
			%%Qualified: Rwp.AdminPage.DoDeleteTeams
			%%Contact: rlittle
			
		----------------------------------------------------------------------------*/
		protected void DoDeleteTeams(object sender, EventArgs e)
		{
            RSR sr = CheckAdmin();

		    if (!sr.Result)
		        {
		        ReportSr(sr, "ipc");
		        return;
		        }
		    // first download the current data...
		    sr = m_apiInterop.CallService<RSR>("api/team/DeleteTeams", true);
			ReportSr(sr, "Delete Teams");
		}

        protected void DoDeleteAuth(object sender, EventArgs e)
        {
            RSR sr = CheckAdmin();

            if (!sr.Result)
            {
                ReportSr(sr, "ipc");
                return;
            }

            // first download the current data...
            sr = m_apiInterop.CallService<RSR>("api/team/DeleteAuthentications", true);
            ReportSr(sr, "Delete Authentications");
        }
        /*----------------------------------------------------------------------------
        	%%Function: DoUploadTeams
        	%%Qualified: Rwp.AdminPage.DoUploadTeams
        	%%Contact: rlittle
        	
            upload a csv file of teams to the server
        ----------------------------------------------------------------------------*/
        protected void DoUploadTeams(object sender, EventArgs e)
        {
            RSR sr;

            if ((fuTeams.PostedFile != null) && (fuTeams.PostedFile.ContentLength > 0))
            {
                HttpContent content = new StreamContent(fuTeams.PostedFile.InputStream);

                sr = m_apiInterop.CallServicePut<RSR>("api/team/PutTeams", content, true);
            }
            else
            {
                sr = new RSR();
                sr.Result = false;
                sr.Reason = String.Format("Upload of file failed!");
                }
            ReportSr(sr, "Upload Teams");
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoUploadSlots
        	%%Qualified: Rwp.AdminPage.DoUploadSlots
        	%%Contact: rlittle
        	
            upload a csv file of slots to the server
        ----------------------------------------------------------------------------*/
        protected void DoUploadSlots(object sender, EventArgs e)
        {
            RSR sr;

            if ((fuSlots.PostedFile != null) && (fuSlots.PostedFile.ContentLength > 0))
            {
                HttpContent content = new StreamContent(fuSlots.PostedFile.InputStream);

                sr = m_apiInterop.CallServicePut<RSR>("api/slot/PutSlots", content, true);
            }
            else
            {
                sr = new RSR();
                sr.Result = false;
                sr.Reason = String.Format("Upload of file failed!");
            }
            ReportSr(sr, "Upload Slots");
        }

        protected void EnableClearItems(object sender, EventArgs e)
        {
            RSR sr = CheckAdmin();

            if (!sr.Result)
                {
                ReportSr(sr, "ipc");
                return;
                }

            btnClearTeams.Enabled = true;
            btnClearAuth.Enabled = true;
        }

		protected void EnableDeleteSlots(object sender, EventArgs e)
		{
            RSR sr = CheckAdmin();

		    if (!sr.Result)
		        {
		        ReportSr(sr, "ipc");
		        return;
		        }

		    btnClearAllSlots.Enabled = true;
			btnClearLastYear.Enabled = true;
		}

        protected void DoShowAddUser(object sender, EventArgs e)
        {
            if (rowAddUser.Visible == false)
            {
                txtAddIdenity.Text = "";
                txtAddTenant.Text = "9188040d-6c67-4c5b-b112-36a304b66dad";
                txtAddTeamName.Text = "";
                txtAddDivision.Text = "";
                txtAddEmail.Text = "";
                chkAddTeam.Checked = false;

            }
            rowAddUser.Visible = !rowAddUser.Visible;
        }

        protected void DoAddUser(object sender, EventArgs e)
        {
            RSR sr = CheckAdmin();

            if (!sr.Result)
            {
                ReportSr(sr, "ipc");
                return;
            }

            string sIdentity = txtAddIdenity.Text;
            string sTenant = txtAddTenant.Text;
            string sTeamName = txtAddTeamName.Text;
            string sDivision = txtAddDivision.Text;
            string sEmail = txtAddEmail.Text;
            bool fAddTeam = chkAddTeam.Checked;

            string sQuery = $"api/team/AddTeamUser?Identity={sIdentity}" +
                            $"&Tenant={sTenant}" +
                            $"&TeamName={sTeamName}" +
                            $"&Division={sDivision}" +
                            $"&Email={sEmail}" +
                            $"&AddTeam={fAddTeam}";

            sr = m_apiInterop.CallService<RSR>(sQuery, true);
            if (sr.Succeeded)
            {
                rowAddUser.Visible = false;
                ReportSr(sr, "AddTeamUser");
            }

            ReportSr(sr, "AddTeamUser");
        }

        protected void CancelAddUser(object sender, EventArgs e)
        {
            rowAddUser.Visible = false;
        }

        // DataGrid support
        protected void DoAddNotice(object sender, EventArgs e)
        {
            RSR sr = CheckAdmin();

            if (!sr.Result)
            {
                ReportSr(sr, "ipc");
                return;
            }

            addID.InnerText = Guid.NewGuid().ToString();
            addCreatedBy.InnerText = m_auth.Identity();
            addCreationDate.InnerText = DateTime.Now.ToString();
            txtDivisionsVisible.Text = "Z";
            txtContentHtml.Text = "";
            addNoticeTable.Visible = true;
        }

        void ClearAndHideAddNotice()
        {
            addNoticeTable.Visible = false;
            txtDivisionsVisible.Text = "";
            txtContentHtml.Text = "";
        }

        protected void DoAddNoticeCancel(object sender, EventArgs e)
        {
            ClearAndHideAddNotice();
        }

        delegate void OnSuccessfulSqlExecution();
        delegate void OnFailedSqlExecution();
        delegate void AfterSqlExecution();
        
        void DoSqlCommandAndReport(string sql, AfterSqlExecution delAfter, OnSuccessfulSqlExecution delSuccess, OnFailedSqlExecution delFailed)
        {
            RSR sr = CheckAdmin();

            if (!sr.Result)
            {
                ReportSr(sr, "ipc");
                return;
            }

            try
            {
                DBConn.Open();

                using (SqlCommand cmdMbrs = DBConn.CreateCommand())
                {
                    cmdMbrs.CommandText = sql;

                    int rows = cmdMbrs.ExecuteNonQuery();

                    if (rows != 1)
                    {
                        if (delFailed != null)
                            delFailed();
                    }
                    else
                    {
                        if (delSuccess != null)
                            delSuccess();
                    }

                    if (delAfter != null)
                        delAfter();
                }
            }
            catch (Exception ex)
            {
                divError.InnerText = ex.Message;
            }
            finally
            {
                DBConn.Close();
            }
        }
        protected void DoAddNoticeSave(object sender, EventArgs e)
        {
            DateTime dttm = DateTime.Parse(addCreationDate.InnerText);

            string sql = $"insert into rwllnotices (ID,CreatedBy,CreationDate,DivisionsVisible,ContentHtml) VALUES ("
                + $"'{Sqlify(addID.InnerText)}',"
                + $"'{Sqlify(addCreatedBy.InnerText)}',"
                + $"'{dttm:M/d/yyyy HH:mm}',"
                + $"'{Sqlify(txtDivisionsVisible.Text)}',"
                + $"'{Sqlify(txtContentHtml.Text)}'"
                + ")";

            DoSqlCommandAndReport(
                sql,
                ClearAndHideAddNotice,
                null,
                () => { divError.InnerText= "failed to add new notice"; });

            BindGrid();
        }

        protected void DataGrid_Delete(object sender, GridViewDeleteEventArgs e)
        {
            ItemData data = GetItemDataFromItem(Notices.Rows[e.RowIndex].Cells);

            string sql = $"delete from rwllnotices where ID='{data.Id}'";

            DoSqlCommandAndReport(
                sql,
                null,
                null,
                () => { divError.InnerText = "failed to delete notice"; });

            BindGrid();
        }

        protected void DataGrid_Edit(object sender, GridViewEditEventArgs e)
        {
            Notices.EditIndex = e.NewEditIndex;
            BindGrid();
        }

        class ItemData
        {
            public string Id { get; set; }
            public string CreatedBy { get; set; }
            public string CreationDate { get; set; }
            public string DivisionsVisible { get; set; }
            public string ContentHtml { get; set; }
        }

        public static string Sqlify(string s)
        {
            return s.Replace("'", "''");
        }

        string GetTextFromControlOrCell(TableCell cell)
        {
            if (cell.Controls != null && cell.Controls.Count > 0)
            {
                return ((TextBox)cell.Controls[0]).Text;
            }

            return cell.Text;
        }

        ItemData GetItemDataFromItem(TableCellCollection cells)
        {
            ItemData itemData = new ItemData();
            
            itemData.Id =                GetTextFromControlOrCell(cells[1]);
            itemData.CreatedBy =         GetTextFromControlOrCell(cells[2]);
            itemData.CreationDate =      GetTextFromControlOrCell(cells[3]);
            itemData.DivisionsVisible  = GetTextFromControlOrCell(cells[4]);
            itemData.ContentHtml =       GetTextFromControlOrCell(cells[5]);

            return itemData;
        }

        protected void DataGrid_Update(object sender, GridViewUpdateEventArgs e)
        {
            ItemData data = GetItemDataFromItem(Notices.Rows[e.RowIndex].Cells);

            if (e.NewValues.Contains("ContentHtml"))
                data.ContentHtml = (string)e.NewValues["ContentHtml"];

            if (e.NewValues.Contains("DivisionsVisible"))
                data.DivisionsVisible = (string)e.NewValues["DivisionsVisible"];

            string sql =
                $"update rwllnotices set ContentHtml='{Sqlify(data.ContentHtml)}', DivisionsVisible='{Sqlify(data.DivisionsVisible)}' where ID = '{Sqlify(data.Id)}'";

            DoSqlCommandAndReport(
                sql,
                null,
                () => { Notices.EditIndex = -1; },
                () => { divError.InnerText = $"failed to update notice {data.Id}"; });

            BindGrid();
        }

        protected void DataGrid_Command(object sender, DataGridCommandEventArgs e)
        {
        }

        protected void DataGrid_Cancel(object sender, GridViewCancelEditEventArgs e)
        {
            Notices.EditIndex = -1;
            BindGrid();
        }

        protected void SortCommand(object sender, DataGridSortCommandEventArgs e)
        {
        }

        protected void BindGrid()
        {
            if (!CheckAdmin().Succeeded)
                return;

            DBConn.Open();

            using (SqlCommand cmdMbrs = DBConn.CreateCommand())
            {
                cmdMbrs.CommandText =
                    "select ID, CreatedBy, CreationDate, ContentHtml, DivisionsVisible from rwllnotices";

                using (SqlDataReader rdrMbrs = cmdMbrs.ExecuteReader())
                {
                    Notices.DataSource = rdrMbrs;
                    Notices.DataBind();
                    rdrMbrs.Close();
                }
            }

            DBConn.Close();
        }

        protected void Notices_OnDataBound(object sender, EventArgs e)
        {
        }
    }
}