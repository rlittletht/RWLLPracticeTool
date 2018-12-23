using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Data.Sql;
using TCore;

// ================================================================================
 // R W P  S V C 
// ================================================================================
namespace RwpApi.Models
{
    // ================================================================================
    // T E A M S 
    // ================================================================================
    public class Teams : TCore.IQueryResult
    {
        List<RwpTeam> m_plrwpt;

        // ================================================================================
        // R W P  T E A M 
        // ================================================================================
        public class RwpTeam
        {
            string m_sName;
            string m_sDivision;
            string m_sPassword;
            DateTime? m_dttmCreated;
            DateTime? m_dttmUpdated;
            int m_cFieldsReleased;
            int m_cCagesReleased;
            int m_cFieldsReleasedToday;
            DateTime? m_dttmReleasedFieldsDate;
            int m_cCagesReleasedToday;
            DateTime? m_dttmReleasedCagesDate;
            string m_sEmail1;
            string m_sEmail2;

            enum iColumns
            {
                iName = 0,
                iDivision,
                iPassword,
                iCreated,
                iUpdated,
                iFieldsReleased,
                iCagesReleased,
                iFieldsReleasedToday,
                iReleasedFieldsDate,
                iCagesReleasedToday,
                iReleasedCagesDate,
                iEmail1,
                iEmail2
            };

            public string Name
            {
                get { return m_sName; }
                set { m_sName = value; }
            }

            public string Division
            {
                get { return m_sDivision; }
                set { m_sDivision = value; }
            }

            public string Password
            {
                get { return m_sPassword; }
                set { m_sPassword = value; }
            }

            public DateTime? Created
            {
                get { return m_dttmCreated; }
                set { m_dttmCreated = value; }
            }

            public DateTime? Updated
            {
                get { return m_dttmUpdated; }
                set { m_dttmUpdated = value; }
            }

            public int FieldsReleased
            {
                get { return m_cFieldsReleased; }
                set { m_cFieldsReleased = value; }
            }

            public int CagesReleased
            {
                get { return m_cCagesReleased; }
                set { m_cCagesReleased = value; }
            }

            public int FieldsReleasedToday
            {
                get { return m_cFieldsReleasedToday; }
                set { m_cFieldsReleasedToday = value; }
            }

            public DateTime? ReleasedFieldsDate
            {
                get { return m_dttmReleasedFieldsDate; }
                set { m_dttmReleasedFieldsDate = value; }
            }

            public int CagesReleasedToday
            {
                get { return m_cCagesReleasedToday; }
                set { m_cCagesReleasedToday = value; }
            }

            public DateTime? ReleasedCagesDate
            {
                get { return m_dttmReleasedCagesDate; }
                set { m_dttmReleasedCagesDate = value; }
            }

            public string Email1
            {
                get { return m_sEmail1; }
                set { m_sEmail1 = value; }
            }

            public string Email2
            {
                get { return m_sEmail2; }
                set { m_sEmail2 = value; }
            }

            /* R W P  T E A M */
            /*----------------------------------------------------------------------------
                %%Function: RwpTeam
                %%Qualified: RwpSvc.Practice:Teams:RwpTeam.RwpTeam
                %%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public RwpTeam(SqlDataReader sqlr)
            {
                m_sName = sqlr.GetString((int) iColumns.iName);
                m_sDivision = sqlr.IsDBNull((int) iColumns.iDivision) ? null : sqlr.GetString((int) iColumns.iDivision);
                m_sPassword = sqlr.IsDBNull((int) iColumns.iPassword) ? null : sqlr.GetString((int) iColumns.iPassword);
                m_dttmCreated = sqlr.IsDBNull((int) iColumns.iCreated)
                    ? (DateTime?) null
                    : sqlr.GetDateTime((int) iColumns.iCreated);
                m_dttmUpdated = sqlr.IsDBNull((int) iColumns.iUpdated)
                    ? (DateTime?) null
                    : sqlr.GetDateTime((int) iColumns.iUpdated);
                m_cFieldsReleased = sqlr.GetInt32((int) iColumns.iFieldsReleased);
                m_cCagesReleased = sqlr.GetInt32((int) iColumns.iCagesReleased);
                m_cFieldsReleasedToday = sqlr.GetInt32((int) iColumns.iFieldsReleasedToday);
                m_dttmReleasedFieldsDate = sqlr.IsDBNull((int) iColumns.iReleasedFieldsDate)
                    ? (DateTime?) null
                    : sqlr.GetDateTime((int) iColumns.iReleasedFieldsDate);
                m_cCagesReleasedToday = sqlr.GetInt32((int) iColumns.iCagesReleasedToday);
                m_dttmReleasedCagesDate = sqlr.IsDBNull((int) iColumns.iReleasedCagesDate)
                    ? (DateTime?) null
                    : sqlr.GetDateTime((int) iColumns.iReleasedCagesDate);
                m_sEmail1 = sqlr.IsDBNull((int) iColumns.iEmail1) ? null : sqlr.GetString((int) iColumns.iEmail1);
                m_sEmail2 = sqlr.IsDBNull((int) iColumns.iEmail2) ? null : sqlr.GetString((int) iColumns.iEmail2);
            }

            public static string s_sSqlQueryString =
                "SELECT " +
                "$$rwllteams$$.TeamName, $$rwllteams$$.Division, $$rwllteams$$.PW, $$rwllteams$$.DateCreated, " +
                "$$rwllteams$$.Dateupdated, $$rwllteams$$.FieldsReleaseCount, $$rwllteams$$.CagesReleaseCount, " +
                "$$rwllteams$$.ReleasedFieldsToday, $$rwllteams$$.ReleasedFieldsDate, $$rwllteams$$.ReleasedCagesToday, " +
                "$$rwllteams$$.ReleasedCagesDate, $$rwllteams$$.Email1, $$rwllteams$$.Email2 " +
                "FROM $$#rwllteams$$";

            public static Dictionary<string, string> s_mpAliases = new Dictionary<string, string>
            {
                {"rwllteams", "RWT"},
            };

            public RwpTeam()
            {
            }

            public static string s_sSqlInsert =
                "INSERT INTO rwllteams " +
                "(TeamName, Division, PW, DateCreated, Dateupdated, FieldsReleaseCount, CagesReleaseCount, ReleasedFieldsToday, ReleasedCagesToday{0})";

            public string SGenerateUpdateQuery(TCore.Sql sql, bool fAdd)
            {
                if (!fAdd)
                    return null;
                string sInsertExtra = "";
                string sValuesExtra = "";
                string sValuesTemplate = "VALUES ('{0}','{1}','{2}','{3}','{4}',{5},{6},{7},{8}{9})";

                // let's figure out if we're going to include ReleasedFieldsDate and ReleasedCagesDate
                if (m_dttmReleasedFieldsDate != null)
                {
                    sInsertExtra += ", ReleasedFieldsDate";
                    sValuesExtra += String.Format(",'{0}'",
                        m_dttmReleasedFieldsDate.Value.ToString("M/d/yyyy HH:mm"));
                }

                if (m_dttmReleasedCagesDate != null)
                {
                    sInsertExtra += ", ReleasedCagesDate";
                    sValuesExtra += String.Format(",'{0}'",
                        m_dttmReleasedCagesDate.Value.ToString("M/d/yyyy HH:mm"));
                }

                if (m_sEmail1 != null)
                {
                    sInsertExtra += ", Email1";
                    sValuesExtra += String.Format(",'{0}'", m_sEmail1);
                }

                if (m_sEmail2 != null)
                {
                    sInsertExtra += ", Email2";
                    sValuesExtra += String.Format(",'{0}'", m_sEmail2);
                }

                string sQueryBase = String.Format(s_sSqlInsert, sInsertExtra);
                string sQueryValues = String.Format(sValuesTemplate, Sql.Sqlify(m_sName),
                    Sql.Sqlify(m_sDivision),
                    Sql.Sqlify(m_sPassword),
                    m_dttmCreated.Value.ToString("M/d/yyyy HH:mm"),
                    m_dttmUpdated.Value.ToString("M/d/yyyy HH:mm"),
                    m_cFieldsReleased, m_cCagesReleased, m_cFieldsReleasedToday,
                    m_cCagesReleasedToday, sValuesExtra);
                return String.Format("{0} {1}", sQueryBase, sQueryValues);
            }
            /* S  G E N  R A N D O M  P A S S W O R D */
            /*----------------------------------------------------------------------------
                %%Function: SGenRandomPassword
                %%Qualified: RwpSvc.Practice:Teams:RwpTeam.SGenRandomPassword
                %%Contact: rlittle

            ----------------------------------------------------------------------------*/

            public static string SGenRandomPassword(Random rnd, string sTeamName)
            {
                int c = 8; // 8 chars
                string sPassword = "";
                int l = System.Environment.TickCount;

                // assuming a structure of {Sport} {Division} {Firstname} {Lastname}, we
                // can make a slightly less random password
                int iNameFirst = sTeamName.IndexOf(' ');
                if (iNameFirst >= 0)
                    iNameFirst = sTeamName.IndexOf(' ', iNameFirst + 1);
                int iNameLast = -1;
                if (iNameFirst >= 0)
                    iNameLast = sTeamName.IndexOf(' ', iNameFirst + 1);

                if (iNameFirst >= 0 && iNameLast >= 0 && iNameFirst + 2 < iNameLast && iNameLast + 2 < sTeamName.Length)
                {
                    sPassword += sTeamName.Substring(iNameFirst + 1, 2).ToLower() +
                                 sTeamName.Substring(iNameLast + 1, 2).ToLower();
                    c = 4;
                }

                char ch;

                for (int i = 0; i < c; i++)
                {
                    int n = rnd.Next(26, 35);
                    if (n < 26)
                    {
                        ch = (char) ('a' + n);
                        sPassword += ch;
                    }

                    if (n >= 26)
                    {
                        ch = (char) ('0' + (n - 26));
                        sPassword += ch;
                    }
                }

                return sPassword;
            }

            /* C H E C K  L E N G T H */
            /*----------------------------------------------------------------------------
                %%Function: CheckLength
                %%Qualified: RwpSvc.Practice:Teams:RwpTeam.CheckLength
                %%Contact: rlittle

            ----------------------------------------------------------------------------*/
            static void CheckLength(string s, string sDesc, int nMax, List<string> plsFail)
            {
                if (s.Length >= nMax)
                    plsFail.Add(String.Format("{0} ({1}) is >= {2} characters", sDesc, s, nMax));
            }

            /* S  R  F R O M  P L S */
            /*----------------------------------------------------------------------------
                %%Function: SRFromPls
                %%Qualified: RwpSvc.Practice:Teams:RwpTeam.SRFromPls
                %%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public static RSR SRFromPls(string sReason, List<string> plsDiff)
            {
                string s = sReason;

                foreach (string sItem in plsDiff)
                    s += sItem + ", ";

                return RSR.Failed(s);
            }

            /* P R E F L I G H T */
            /*----------------------------------------------------------------------------
                %%Function: Preflight
                %%Qualified: RwpSvc.Practice:Teams:RwpTeam.Preflight
                %%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public RSR Preflight(TCore.Sql sql)
            {
                List<string> plsFail = new List<string>();

                CheckLength(m_sName, "TeamName", 50, plsFail);
                CheckLength(m_sDivision, "Division", 10, plsFail);
                CheckLength(m_sPassword, "PW", 20, plsFail);

                if (plsFail.Count > 0)
                    return SRFromPls("preflight failed", plsFail);

                return RSR.Success();
            }
        }

        // ================================================================================
        // C S V  T E A M S 
        // ================================================================================
        public class CsvTeams : Csv
        {
            readonly string[] m_rgsStaticHeaderTeams = new string[]
            {
                "TeamName", "Division", "PW", "DateCreated", "DateUpdated", "FieldsReleaseCount", "CagesReleaseCount",
                "ReleasedFieldsToday", "ReleasedFieldsDate", "ReleasedCagesToday", "ReleasedCagesDate", "Email1",
                "Email2"
            };

            /* C S V  T E A M S */
            /*----------------------------------------------------------------------------
                %%Function: CsvTeams
                %%Qualified: RwpSvc.Practice:Teams:CsvTeams.CsvTeams
                %%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public CsvTeams()
            {
                SetStaticHeader(m_rgsStaticHeaderTeams);
            }

            string StringValOrNull(string s)
            {
                return s == null ? "(NULL)" : s;
            }

            string DttmValOrNull(DateTime? dttm)
            {
                return dttm == null ? "(NULL)" : dttm.Value.ToString();
            }

            /* C S V  M A K E */
            /*----------------------------------------------------------------------------
                %%Function: CsvMake
                %%Qualified: RwpSvc.Practice:Teams:CsvTeams.CsvMake
                %%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public string CsvMake(RwpTeam rwpt)
            {
                Dictionary<string, string> mpColData = new Dictionary<string, string>();

                mpColData.Add("TeamName", rwpt.Name);
                mpColData.Add("Division", rwpt.Division);
                mpColData.Add("PW", rwpt.Password);
                mpColData.Add("DateCreated", DttmValOrNull(rwpt.Created));
                mpColData.Add("DateUpdated", DttmValOrNull(rwpt.Updated));
                mpColData.Add("FieldsReleaseCount", rwpt.FieldsReleased.ToString());
                mpColData.Add("CagesReleaseCount", rwpt.CagesReleased.ToString());
                mpColData.Add("ReleasedFieldsToday", rwpt.FieldsReleasedToday.ToString());
                mpColData.Add("ReleasedFieldsDate", DttmValOrNull(rwpt.ReleasedFieldsDate));
                mpColData.Add("ReleasedCagesToday", rwpt.CagesReleasedToday.ToString());
                mpColData.Add("ReleasedCagesDate", DttmValOrNull(rwpt.ReleasedCagesDate));
                mpColData.Add("Email1", StringValOrNull(rwpt.Email1));
                mpColData.Add("Email2", StringValOrNull(rwpt.Email2));

                return CsvMake(mpColData);
            }

            /* L O A D  R W P T  F R O M  C S V */
            /*----------------------------------------------------------------------------
                %%Function: LoadRwptFromCsv
                %%Qualified: RwpSvc.Practice:Teams:CsvTeams.LoadRwptFromCsv
                %%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public RSR LoadRwptFromCsv(string sLine, TCore.Sql sql, out RwpTeam rwpt, out bool fAdd,
                out List<string> plsDiff)
            {
                string[] rgs = LineToArray(sLine);
                SqlWhere sw = new SqlWhere();
                fAdd = true;
                rwpt = new RwpTeam();
                plsDiff = new List<string>();

                sw.AddAliases(RwpTeam.s_mpAliases);
                try
                {
                    rwpt.Name = GetStringVal(rgs, "TEAMNAME");
                    if (rwpt.Name == "")
                        return RSR.Success();

                    sw.Add(String.Format("$$rwllteams$$.TeamName = '{0}'", Sql.Sqlify(rwpt.Name)), SqlWhere.Op.And);
                    SqlReader sqlr = new SqlReader(sql);
                    if (sqlr.FExecuteQuery(sw.GetWhere(RwpTeam.s_sSqlQueryString), Startup._sResourceConnString)
                        && sqlr.Reader.Read())
                    {
                        sqlr.Close();
                        // found a match.  for now, this is an error
                        throw new Exception(String.Format("team name {0} already exists", rwpt.Name));
                    }

                    sqlr.Close();

                    rwpt.Division = GetStringValNullable(rgs, "DIVISION");
                    rwpt.Password = GetStringValNullable(rgs, "PW");
                    rwpt.Created = GetDateValNullable(rgs, "DATECREATED");
                    rwpt.Updated = GetDateValNullable(rgs, "DATEUPDATED");
                    rwpt.FieldsReleased = GetIntVal(rgs, "FIELDSRELEASECOUNT");
                    rwpt.CagesReleased = GetIntVal(rgs, "CAGESRELEASECOUNT");
                    rwpt.FieldsReleasedToday = GetIntVal(rgs, "RELEASEDFIELDSTODAY");
                    rwpt.CagesReleasedToday = GetIntVal(rgs, "RELEASEDCAGESTODAY");
                    rwpt.ReleasedFieldsDate = GetDateValNullable(rgs, "RELEASEDFIELDSDATE");
                    rwpt.ReleasedCagesDate = GetDateValNullable(rgs, "RELEASEDCAGESDATE");
                    rwpt.Email1 = GetStringValNullable(rgs, "EMAIL1");
                    rwpt.Email2 = GetStringValNullable(rgs, "EMAIL2");
                }
                catch (Exception e)
                {
                    return RSR.Failed(e);
                }

                return RSR.Success();
            }
        }


        /* F  A D D  R E S U L T  R O W */
        /*----------------------------------------------------------------------------
            %%Function: FAddResultRow
            %%Qualified: RwpSvc.Practice:Teams.FAddResultRow
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        public bool FAddResultRow(SqlReader sqlr, int iRecordSet)
        {
            m_plrwpt.Add(new RwpTeam(sqlr.Reader));
            return true;
        }

        /* G E T  C S V */
        /*----------------------------------------------------------------------------
            %%Function: GetCsv
            %%Qualified: RwpSvc.Practice:Teams.GetCsv
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        public RSR GetCsv(Stream stm)
        {
            CsvTeams csvt = new CsvTeams();
            SqlWhere sw = new SqlWhere();
            TextWriter tw = new StreamWriter(stm);

            sw.AddAliases(RwpTeam.s_mpAliases);

            m_plrwpt = new List<RwpTeam>();
//                StringBuilder sb = new StringBuilder(4096);

            RSR sr = RSR.FromSR(Sql.ExecuteQuery(sw.GetWhere(RwpTeam.s_sSqlQueryString), this, Startup._sResourceConnString));
            tw.WriteLine(csvt.Header());
            foreach (RwpTeam rwpt in m_plrwpt)
                tw.WriteLine(csvt.CsvMake(rwpt));

            tw.Flush();
            return RSR.Success();
        }

        /* I M P O R T  C S V */
        /*----------------------------------------------------------------------------
            %%Function: ImportCsv
            %%Qualified: RwpSvc.Practice:Teams.ImportCsv
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        public static RSR ImportCsv(Stream stm)
        {
            RSR sr;
            Sql sql;
            CsvTeams csv = new CsvTeams();
            Random rnd = new Random(System.Environment.TickCount);

            TextReader tr = new StreamReader(stm);

            // process each line
            sr = RSR.FromSR(Sql.OpenConnection(out sql, Startup._sResourceConnString));
            if (!sr.Result)
                return sr;

            sr = RSR.FromSR(sql.BeginTransaction());
            if (!sr.Result)
                return sr;

            bool fHeadingRead = false;
            string sLine;
            int iLine = 0;
            RwpTeam rwpt;
            bool fAdd;
            List<string> plsDiff;

            try
            {
                while ((sLine = tr.ReadLine()) != null)
                {
                    iLine++;
                    if (sLine.Length < 2)
                        continue;

                    if (!fHeadingRead)
                    {
                        sr = csv.ReadHeaderFromString(sLine);
                        if (!sr.Result)
                            throw new Exception(String.Format("Failed to make heading at line {0}: {1}", iLine - 1,
                                sr.Reason));
                        fHeadingRead = true;
                        continue;
                    }

                    sr = csv.LoadRwptFromCsv(sLine, sql, out rwpt, out fAdd, out plsDiff);
                    if (!sr.Result)
                        throw new Exception(String.Format("Failed to process line {0}: {1}", iLine - 1, sr.Reason));

                    if (rwpt.Name == "")
                        continue;

                    // at this point, rwpt is a fully loaded team; check for errors and generate a passowrd if necessary
                    sr = rwpt.Preflight(sql);
                    if (!sr.Result)
                        throw new Exception(String.Format("Failed to preflight line {0}: {1}", iLine - 1, sr.Reason));

                    if (string.IsNullOrEmpty(rwpt.Password))
                        rwpt.Password = RwpTeam.SGenRandomPassword(rnd, rwpt.Name);

                    if (rwpt.Created == null)
                        rwpt.Created = DateTime.Now;

                    if (rwpt.Updated == null)
                        rwpt.Updated = rwpt.Created;

                    if (rwpt.ReleasedCagesDate == null)
                        rwpt.ReleasedCagesDate = DateTime.Parse("1/1/2013");

                    if (rwpt.ReleasedFieldsDate == null)
                        rwpt.ReleasedFieldsDate = DateTime.Parse("1/1/2013");

                    // at this point, we would insert...
                    string sInsert = rwpt.SGenerateUpdateQuery(sql, fAdd);

                    SqlCommand sqlcmd = sql.CreateCommand();
                    sqlcmd.CommandText = sInsert;
                    sqlcmd.Transaction = sql.Transaction;
                    sqlcmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                sql.Rollback();
                sql.Close();

                return RSR.Failed(e);
            }

            sql.Commit();
            sql.Close();
            return RSR.Success();
        }

        public static RSR ClearTeams()
        {
            return RSR.FromSR(Sql.ExecuteNonQuery("DELETE FROM rwllteams WHERE TeamName <> 'Administrator'",
                Startup._sResourceConnString));
        }
    }
}