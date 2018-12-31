using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Data.Sql;
using NUnit.Framework;
using TCore;

// ================================================================================
 // R W P  S V C 
// ================================================================================
namespace RwpApi
{
    // ================================================================================
    // S L O T S
    // ================================================================================
    public class RwpSlots : TCore.IQueryResult
    {
        List<RwpSlot> m_plrwps;

        public RwpSlots()
        {
            m_plrwps = new List<RwpSlot>();
        }

        public List<RwpSlot> Slots
        {
            get { return m_plrwps; }
        }

        /*----------------------------------------------------------------------------
            %%Function: StartTimeFromDateAndTime
            %%Qualified: RwpSvc.Practice.RwpSlots.RwpSlot.StartTimeFromDateAndTime
            %%Contact: rlittle

            FUTURE: The timezone handling is DUMB! We should store the timezone
            offset in the database so we don't have to guess about it here. Just
            add "-0800" for PST and "-0700" for PDT and so on into a single column.
            This could be taken care of at slot upload time and it solves this whole
            problem

            We aren't doing this now because the database is LIVE and I just don't 
            want to deal with changing the format while its live.  But the exception
            below will be hit if I don't remember to fix this in the offseason.
        ----------------------------------------------------------------------------*/
        public static DateTime StartTimeFromDateAndTime(DateTime dttmStartDate, string sTime)
        {
            Tuple<DateTime, DateTime>[] rgDaylightSavingsRanges = new Tuple<DateTime, DateTime>[]
            {
                new Tuple<DateTime, DateTime>(DateTime.Parse("3/10/2019"), DateTime.Parse("11/3/2019")),
                new Tuple<DateTime, DateTime>(DateTime.Parse("3/11/2018"), DateTime.Parse("11/4/2018")),
                new Tuple<DateTime, DateTime>(DateTime.Parse("3/12/2017"), DateTime.Parse("11/5/2017")),
                new Tuple<DateTime, DateTime>(DateTime.Parse("3/10/2016"), DateTime.Parse("11/6/2016")),
                new Tuple<DateTime, DateTime>(DateTime.Parse("3/8/2015"), DateTime.Parse("11/1/2015")),
            };
            
            if (dttmStartDate < DateTime.Parse("3/7/2015")
                || dttmStartDate > DateTime.Parse("11/3/2019"))
                throw new Exception(
                    "cannot handle timezone conversion. did you forget to add the 'TimeZone' column in the offseason?!?");

            // now, convert this to UTC
            int nHours = -8;

            foreach (Tuple<DateTime, DateTime> range in rgDaylightSavingsRanges)
            {
                if (dttmStartDate >= range.Item1 && dttmStartDate <= range.Item2)
                {
                    nHours = -7;
                    break;
                }
            }

            DateTime dttmParse = DateTime.Parse(String.Format("{0} {1}", dttmStartDate.ToString("d"), sTime));
            DateTime dttmUTC = new DateTime(dttmParse.Year, dttmParse.Month, dttmParse.Day, dttmParse.Hour,
                dttmParse.Minute, 0, DateTimeKind.Utc);

            return dttmUTC.AddHours(-nHours);
        }

        [Test]
        [TestCase("2/18/2017", "07:30:00 PM", "Sun, 19 Feb 2017 03:30:00 GMT")] // before PDT
        [TestCase("3/18/2017", "07:30:00 PM", "Sun, 19 Mar 2017 02:30:00 GMT")] // after PDT
        [TestCase("3/11/2017", "07:30:00 PM", "Sun, 12 Mar 2017 03:30:00 GMT")] // before PDT
        [TestCase("3/12/2017", "07:30:00 PM", "Mon, 13 Mar 2017 02:30:00 GMT")] // after PDT
        public static void TestStartTimeFromDateAndTime(string sStartDate, string sTime, string sExpected)
        {
            DateTime dttmStart = DateTime.Parse(sStartDate);
            DateTime dttmActual = RwpSlots.StartTimeFromDateAndTime(dttmStart, sTime);

            Assert.AreEqual(sExpected, dttmActual.ToString("r"));
        }

        public static RSR_CalItems GetCalendarItemsForTeam(string sLinkID)
        {
            SqlWhere sw = new SqlWhere();
            RSR rsr;
            RSR_CalItems rci;
            RwpSlots slots = new RwpSlots();

            sw.AddAliases(RwpSlot.s_mpAliases);
            try
            {
                sw.Add(String.Format("$$rwllpractice$$.Reserved = (select TeamID from rwllcalendarlinks where linkid='{0}')", Sql.Sqlify(sLinkID)), SqlWhere.Op.And);

                rsr = RSR.FromSR(Sql.ExecuteQuery(null, sw.GetWhere(RwpSlot.s_sSqlQueryString), slots,
                    Startup._sResourceConnString));

                if (!rsr.Succeeded)
                {
                    rci = RSR_CalItems.FromRSR(rsr);
                    rci.Reason = String.Format("{0} {1}", rci.Reason, Startup._sResourceConnString);
                    return rci;
                }

                rci = RSR_CalItems.FromRSR(RSR.FromSR(SR.Success()));

                List<CalItem> plci = new List<CalItem>();

                if (slots.Slots != null)
                {
                    foreach (RwpSlot slot in slots.Slots)
                    {
                        CalItem ci = new CalItem();

                        ci.Start = StartTimeFromDateAndTime(slot.SlotDate, slot.StartTime);
                        ci.End = StartTimeFromDateAndTime(slot.SlotDate, slot.EndTime);
                        ci.Location = String.Format("{0}: {1}", slot.Venue, slot.Field);
                        ci.Title = String.Format("Team Practice: {0}", slot.Reserved);
                        ci.Description =
                            String.Format("Redmond West Little League team practice at {0} ({1}), for team {2}",
                                slot.Venue, slot.Field, slot.Reserved);
                        ci.UID = String.Format("{0}-rwllpractice-{1}", slot.Slot, slot.SlotDate.ToString("yyyyMMdd"));
                        plci.Add(ci);
                    }
                }

                rci.TheValue = plci;
                return rci;
            }
            catch (Exception e)
            {
                rci = RSR_CalItems.FromRSR(RSR.Failed(e));
                rci.Reason = String.Format("{0} ({1})", rci.Reason, sLinkID);
                return rci;
            }
        }

        // ================================================================================
        // R W P  S L O T S
        // ================================================================================
        public class RwpSlot
        {
            int m_nSlot;
            double m_flWeek;
            string m_sStatus;
            string m_sVenue;
            string m_sField;
            DateTime m_dttmSlotDate;
            string m_sWeekday;
            string m_sStartTime;
            string m_sEndTime;
            string m_sHours;
            string m_sReserved;
            string m_sDivisions;
            DateTime? m_dttmReserved;
            string m_sType;
            DateTime? m_dttmReleased;
            string m_sReleaseTeam;

            enum iColumns
            {
                iSlot = 0,
                iWeek,
                iStatus,
                iVenue,
                iField,
                iSlotDate,
                iWeekday,
                iStartTime,
                iEndTime,
                iHour,
                iReserved,
                iDivisions,
                iReservedDate,
                iType,
                iReleased,
                iReleaseTeam,
                iName = 0
            };

            public int Slot
            {
                get { return m_nSlot; }
                set { m_nSlot = value; }
            }

            public double Week
            {
                get { return m_flWeek; }
                set { m_flWeek = value; }
            }

            public string Status
            {
                get { return m_sStatus; }
                set { m_sStatus = value; }
            }

            public string Venue
            {
                get { return m_sVenue; }
                set { m_sVenue = value; }
            }

            public string Field
            {
                get { return m_sField; }
                set { m_sField = value; }
            }

            public DateTime SlotDate
            {
                get { return m_dttmSlotDate; }
                set { m_dttmSlotDate = value; }
            }

            public string Weekday
            {
                get { return m_sWeekday; }
                set { m_sWeekday = value; }
            }

            public string StartTime
            {
                get { return m_sStartTime; }
                set { m_sStartTime = value; }
            }

            public string EndTime
            {
                get { return m_sEndTime; }
                set { m_sEndTime = value; }
            }

            public string Hours
            {
                get { return m_sHours; }
                set { m_sHours = value; }
            }

            public string Reserved
            {
                get { return m_sReserved; }
                set { m_sReserved = value; }
            }

            public string Divisions
            {
                get { return m_sDivisions; }
                set { m_sDivisions = value; }
            }

            public DateTime? ReservedDate
            {
                get { return m_dttmReserved; }
                set { m_dttmReserved = value; }
            }

            public string Type
            {
                get { return m_sType; }
                set { m_sType = value; }
            }

            public DateTime? Released
            {
                get { return m_dttmReleased; }
                set { m_dttmReleased = value; }
            }

            public string ReleaseTeam
            {
                get { return m_sReleaseTeam; }
                set { m_sReleaseTeam = value; }
            }

            /* R W P  S L O T */
            /*----------------------------------------------------------------------------
                %%Function: RwpSlot
                %%Qualified: RwpSvc.Practice:RwpSlots:RwpSlot.RwpSlot
                %%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public RwpSlot(SqlDataReader sqlr)
            {
                m_nSlot = sqlr.GetInt32((int) iColumns.iSlot);
                m_flWeek = sqlr.GetDouble((int) iColumns.iWeek);
                m_sStatus = sqlr.GetString((int) iColumns.iStatus);
                m_sVenue = sqlr.GetString((int) iColumns.iVenue);
                m_sField = sqlr.GetString((int) iColumns.iField);
                m_dttmSlotDate = sqlr.GetDateTime((int) iColumns.iSlotDate);
                m_sWeekday = sqlr.GetString((int) iColumns.iWeekday);
                m_sStartTime = sqlr.GetString((int) iColumns.iStartTime);
                m_sEndTime = sqlr.GetString((int) iColumns.iEndTime);
                m_sHours = sqlr.GetString((int) iColumns.iHour);
                m_sReserved = sqlr.GetString((int) iColumns.iReserved);
                m_sDivisions = sqlr.GetString((int) iColumns.iDivisions);
                m_dttmReserved = sqlr.IsDBNull((int) iColumns.iReservedDate)
                    ? (DateTime?) null
                    : sqlr.GetDateTime((int) iColumns.iReservedDate);
                m_sType = sqlr.GetString((int) iColumns.iType);
                m_dttmReleased = sqlr.IsDBNull((int) iColumns.iReleased)
                    ? (DateTime?) null
                    : sqlr.GetDateTime((int) iColumns.iReleased);
                m_sReleaseTeam = sqlr.IsDBNull((int) iColumns.iReleaseTeam)
                    ? null
                    : sqlr.GetString((int) iColumns.iReleaseTeam);
            }

            public static string s_sSqlQueryString =
                "SELECT " +
                "$$rwllpractice$$.SlotNo, $$rwllpractice$$.Week, $$rwllpractice$$.Status, $$rwllpractice$$.Venue, " +
                "$$rwllpractice$$.Field, $$rwllpractice$$.Date, $$rwllpractice$$.Weekday, $$rwllpractice$$.StartTime, " +
                "$$rwllpractice$$.EndTime, $$rwllpractice$$.Hours, $$rwllpractice$$.Reserved, " +
                "$$rwllpractice$$.Divisions, $$rwllpractice$$.ReserveDatetime, $$rwllpractice$$.Type, " +
                "$$rwllpractice$$.ReleaseDatetime, $$rwllpractice$$.ReleaseTeam " +
                "FROM $$#rwllpractice$$";

            public static Dictionary<string, string> s_mpAliases = new Dictionary<string, string>
            {
                {"rwllpractice", "RWP"},
            };

            public RwpSlot()
            {
            }

            public static string s_sSqlInsert =
                "INSERT INTO rwllpractice " +
                "(SlotNo, Week, Status, Venue, Field, Date, Weekday, StartTime, EndTime, Hours, Reserved, " +
                "Divisions, Type{0}) "; // We will append ReserveDatetime, ReleaseDatetime, ReleaseTeam as needed

            public string SGenerateUpdateQuery(TCore.Sql sql, bool fAdd)
            {
                if (!fAdd)
                    return null;
                string sInsertExtra = "";
                string sValuesExtra = "";
                string sValuesTemplate =
                    "VALUES ({0},{1},'{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}'{13})";

                // let's figure out if we're going to include ReserveDatetime, ReleaseDatetime, ReleaseTeam
                if (m_dttmReserved != null)
                {
                    sInsertExtra += ", ReserveDatetime";
                    sValuesExtra += String.Format(",'{0}'",
                        m_dttmReserved.Value.ToString("M/d/yyyy HH:mm"));
                }

                if (m_dttmReleased != null)
                {
                    sInsertExtra += ", ReleaseDatetime";
                    sValuesExtra += String.Format(",'{0}'",
                        m_dttmReleased.Value.ToString("M/d/yyyy HH:mm"));
                }

                if (m_sReleaseTeam != null)
                {
                    sInsertExtra += ", ReleaseTeam";
                    sValuesExtra += String.Format(",'{0}'", m_sReleaseTeam);
                }

                string sQueryBase = String.Format(s_sSqlInsert, sInsertExtra);
                string sQueryValues = String.Format(sValuesTemplate, m_nSlot, m_flWeek, m_sStatus, Sql.Sqlify(m_sVenue),
                    Sql.Sqlify(m_sField), m_dttmSlotDate.ToString("M/d/yyyy HH:mm"),
                    m_sWeekday, m_sStartTime, m_sEndTime, m_sHours, Sql.Sqlify(m_sReserved),
                    Sql.Sqlify(m_sDivisions), m_sType, sValuesExtra);

                return String.Format("{0} {1}", sQueryBase, sQueryValues);
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


            public static List<string> s_plsWeekdays = new List<string>
            {
                "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN"
            };

            public static List<string> s_plsTypes = new List<string>
            {
                "FIELD", "CAGE"
            };

            /* P R E F L I G H T */
            /*----------------------------------------------------------------------------
                %%Function: Preflight
                %%Qualified: RwpSvc.Practice:Teams:RwpTeam.Preflight
                %%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public RSR Preflight(TCore.Sql sql)
            {
                List<string> plsFail = new List<string>();

                CheckLength(m_sStatus, "Status", 255, plsFail);
                CheckLength(m_sVenue, "Venue", 255, plsFail);
                CheckLength(m_sField, "Field", 255, plsFail);
                if (!s_plsWeekdays.Contains(m_sWeekday.ToUpper()))
                    plsFail.Add(String.Format("Weekday '{0}' is not valid", m_sWeekday));
                CheckLength(m_sReserved, "Reserved", 255, plsFail);
                CheckLength(m_sDivisions, "Divisions", 15, plsFail);
                CheckLength(m_sType, "Type", 50, plsFail);
                if (!s_plsTypes.Contains(m_sType.ToUpper()))
                    plsFail.Add(String.Format("Type '{0}' is not valid", m_sType));

                if (plsFail.Count > 0)
                    return SRFromPls("preflight failed", plsFail);

                return RSR.Success();
            }
        }

        // ================================================================================
        // C S V  S L O T S
        // ================================================================================
        public class CsvSlots : Csv
        {
            readonly string[] m_rgsStaticHeaderSlots = new string[]
            {
                "SlotNo", "Week", "Status", "Venue", "Field", "Date", "Weekday", "StartTime", "EndTime", "Hours",
                "Reserved", "Divisions", "ReserveDatetime", "Type", "ReleaseDatetime", "ReleaseTeam"
            };

            /* C S V  T E A M S */
            /*----------------------------------------------------------------------------
                %%Function: CsvTeams
                %%Qualified: RwpSvc.Practice:Teams:CsvTeams.CsvTeams
                %%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public CsvSlots()
            {
                SetStaticHeader(m_rgsStaticHeaderSlots);
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
                %%Qualified: RwpSvc.Practice:RwpSlots:CsvSlots.CsvMake
                %%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public string CsvMake(RwpSlot rwps)
            {
                Dictionary<string, string> mpColData = new Dictionary<string, string>();

                mpColData.Add("SlotNo", rwps.Slot.ToString());
                mpColData.Add("Week", rwps.Week.ToString());
                mpColData.Add("Status", rwps.Status);
                mpColData.Add("Venue", rwps.Venue);
                mpColData.Add("Field", rwps.Field);
                mpColData.Add("Date", DttmValOrNull(rwps.SlotDate));
                mpColData.Add("Weekday", rwps.Weekday);
                mpColData.Add("StartTime", rwps.StartTime);
                mpColData.Add("EndTime", rwps.EndTime);
                mpColData.Add("Hours", rwps.Hours);
                mpColData.Add("Reserved", rwps.Reserved);
                mpColData.Add("Divisions", rwps.Divisions);
                mpColData.Add("ReserveDatetime", DttmValOrNull(rwps.ReservedDate));
                mpColData.Add("Type", rwps.Type);
                mpColData.Add("ReleaseDatetime", DttmValOrNull(rwps.Released));
                mpColData.Add("ReleaseTeam", StringValOrNull(rwps.ReleaseTeam));

                return CsvMake(mpColData);
            }

            /* L O A D  R W P T  F R O M  C S V */
            /*----------------------------------------------------------------------------
                %%Function: LoadRwptFromCsv
                %%Qualified: RwpSvc.Practice:RwpSlots:CsvSlots.LoadRwptFromCsv
                %%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public RSR LoadRwpsFromCsv(string sLine, Sql sql, out RwpSlot rwps, out bool fAdd, out List<string> plsDiff)
            {
                string[] rgs = LineToArray(sLine);
                SqlWhere sw = new SqlWhere();
                fAdd = true;
                plsDiff = new List<string>();
                rwps = null;

                if (rgs[0] == "")
                    return RSR.Success();

                rwps = new RwpSlot();

                sw.AddAliases(RwpSlot.s_mpAliases);
                try
                {
                    rwps.Slot = GetIntVal(rgs, "SLOTNO");

                    sw.Add(String.Format("$$rwllpractice$$.SlotNo = {0}", rwps.Slot), SqlWhere.Op.And);
                    SqlReader sqlr = new SqlReader(sql);
                    if (sqlr.FExecuteQuery(sw.GetWhere(RwpSlot.s_sSqlQueryString), Startup._sResourceConnString)
                        && sqlr.Reader.Read())
                    {
                        sqlr.Close();
                        // found a match.  for now, this is an error
                        throw new Exception(String.Format("slot {0} already exists", rwps.Slot));
                    }

                    sqlr.Close();

                    rwps.Slot = GetIntVal(rgs, "SLOTNO");
                    rwps.Week = GetDoubleVal(rgs, "WEEK");
                    rwps.Status = GetStringVal(rgs, "STATUS");
                    rwps.Venue = GetStringVal(rgs, "VENUE");
                    rwps.Field = GetStringVal(rgs, "FIELD");
                    rwps.SlotDate = GetDateVal(rgs, "DATE");
                    rwps.Weekday = GetStringVal(rgs, "WEEKDAY");
                    rwps.StartTime = GetStringVal(rgs, "STARTTIME");
                    rwps.EndTime = GetStringVal(rgs, "ENDTIME");
                    rwps.Hours = GetStringVal(rgs, "HOURS");
                    rwps.Reserved = GetStringVal(rgs, "RESERVED");
                    rwps.Divisions = GetStringVal(rgs, "DIVISIONS");
                    rwps.ReservedDate = GetDateValNullable(rgs, "RESERVEDATETIME");
                    rwps.Type = GetStringVal(rgs, "TYPE");
                    rwps.Released = GetDateValNullable(rgs, "RELEASEDATETIME");
                    rwps.ReleaseTeam = GetStringValNullable(rgs, "RELEASETEAM");
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
            %%Qualified: RwpSvc.Practice:RwpSlots.FAddResultRow
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        public bool FAddResultRow(SqlReader sqlr, int iRecordSet)
        {
            m_plrwps.Add(new RwpSlot(sqlr.Reader));
            return true;
        }

        /* G E T  C S V */
        /*----------------------------------------------------------------------------
            %%Function: GetCsv
            %%Qualified: RwpSvc.Practice:RwpSlots.GetCsv
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        public SR GetCsv(Stream stm)
        {
            CsvSlots csvs = new CsvSlots();
            SqlWhere sw = new SqlWhere();
            TextWriter tw = new StreamWriter(stm);

            sw.AddAliases(RwpSlot.s_mpAliases);

            m_plrwps = new List<RwpSlot>();

            SR sr = Sql.ExecuteQuery(sw.GetWhere(RwpSlot.s_sSqlQueryString), this, Startup._sResourceConnString);
            tw.WriteLine(csvs.Header());
            foreach (RwpSlot rwps in m_plrwps)
                tw.WriteLine(csvs.CsvMake(rwps));

            tw.Flush();
            return SR.Success();
        }

        /* I M P O R T  C S V */
        /*----------------------------------------------------------------------------
            %%Function: ImportCsv
            %%Qualified: RwpSvc.Practice:RwpSlots.ImportCsv
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        public static RSR ImportCsv(Stream stm)
        {
            RSR sr;
            Sql sql;
            CsvSlots csv = new CsvSlots();

            TextReader tr = new StreamReader(stm);

            // process each line
            sr = RSR.FromSR(Sql.OpenConnection(out sql, Startup._sResourceConnString));
            if (!sr.Result)
                return sr;

            sr = RSR.FromSR(sql.BeginTransaction());
            if (!sr.Result)
            {
                sql.Close();
                return sr;
            }

            bool fHeadingRead = false;
            string sLine;
            int iLine = 0;
            RwpSlot rwps;
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

                    sr = csv.LoadRwpsFromCsv(sLine, sql, out rwps, out fAdd, out plsDiff);
                    if (!sr.Result)
                        throw new Exception(String.Format("Failed to process line {0}: {1}", iLine - 1, sr.Reason));

                    if (rwps == null) // this means it was an empty csv line
                        continue;

                    // at this point, rwps is a fully loaded team; check for errors and generate a passowrd if necessary
                    sr = rwps.Preflight(sql);
                    if (!sr.Result)
                        throw new Exception(String.Format("Failed to preflight line {0}: {1}", iLine - 1, sr.Reason));

                    // at this point, we would insert...
                    string sInsert = rwps.SGenerateUpdateQuery(sql, fAdd);

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

        public static RSR ClearAll()
        {
            return RSR.FromSR(Sql.ExecuteNonQuery("DELETE FROM rwllpractice", Startup._sResourceConnString));
        }

        public static RSR ClearYear(int nYear)
        {
            return RSR.FromSR(Sql.ExecuteNonQuery(
                String.Format("DELETE FROM rwllpractice WHERE ([Date] >= '{0}-01-01' And [Date] <= '{0}-12-31')",
                    nYear), Startup._sResourceConnString));
        }
    }
}