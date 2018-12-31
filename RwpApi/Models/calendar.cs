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

namespace RwpApi
{
    // ================================================================================
    // C A L E N D A R  L I N K S
    // ================================================================================
    public class CalendarLinks : TCore.IQueryResult
    {
        List<CalendarLinkItem> m_plcall;

        public CalendarLinks()
        {
            m_plcall = new List<CalendarLinkItem>();
        }

        public List<CalendarLinkItem> Links => m_plcall;

        /*----------------------------------------------------------------------------
        	%%Function: GetCalendarLinksForTeam
        	%%Qualified: RwpApi.CalendarLinks.GetCalendarLinksForTeam
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public static RSR_CalendarLinks GetCalendarLinksForTeam(string sTeam)
        {
            SqlWhere sw = new SqlWhere();
            RSR rsr;
            RSR_CalendarLinks rci;
            CalendarLinks links = new CalendarLinks();

            sw.AddAliases(CalendarLinkItem.s_mpAliases);
            try
            {
                sw.Add(String.Format("$$rwllcalendarlinks$$.TeamID = '{0}'", Sql.Sqlify(sTeam)), SqlWhere.Op.And);

                rsr = RSR.FromSR(Sql.ExecuteQuery(null, sw.GetWhere(RwpSlots.RwpSlot.s_sSqlQueryString), links,
                    Startup._sResourceConnString));

                if (!rsr.Succeeded)
                {
                    rci = RSR_CalendarLinks.FromRSR(rsr);
                    rci.Reason = String.Format("{0} {1}", rci.Reason, Startup._sResourceConnString);
                    return rci;
                }

                rci = RSR_CalendarLinks.FromRSR(RSR.FromSR(SR.Success()));

                List<CalendarLink> plcall = new List<CalendarLink>();

                if (links.Links != null)
                {
                    foreach (CalendarLinkItem linkItem in links.Links)
                    {
                        CalendarLink link = new CalendarLink()
                        {
                            Link = linkItem.Link, Team = linkItem.Team, Authority = linkItem.Authority,
                            CreateDate = linkItem.CreateDate, Comment = linkItem.Comment
                        };
                        plcall.Add(link);
                    }
                }

                rci.TheValue = plcall;
                return rci;
            }

            catch (Exception e)
            {
                rci = RSR_CalendarLinks.FromRSR(RSR.Failed(e));
                rci.Reason = String.Format("{0} ({1})", rci.Reason, sTeam);
                return rci;
            }
        }

        // ================================================================================
        // C A L E N D A R  L I N K  I T E M
        // ================================================================================
        public class CalendarLinkItem
        {
            private Guid m_guidLink;
            private string m_sTeam;
            private string m_sAuthority;
            private DateTime m_dttmCreateDate;
            private string m_sComment;

            enum iColumns
            {
                iLink = 0,
                iTeam,
                iAuthority,
                iCreateDate,
                iComment,
            };

            public Guid Link => m_guidLink;
            public string Team => m_sTeam;
            public string Authority => m_sAuthority;
            public DateTime CreateDate => m_dttmCreateDate;
            public string Comment => m_sComment;

            /*----------------------------------------------------------------------------
            	%%Function: CalendarLinkItem
            	%%Qualified: RwpApi.CalendarLinks.CalendarLinks
            	%%Contact: rlittle
            	
            ----------------------------------------------------------------------------*/
            public CalendarLinkItem(SqlDataReader sqlr)
            {
                m_guidLink = sqlr.GetGuid((int) iColumns.iLink);
                m_sTeam = sqlr.GetString((int) iColumns.iTeam);
                m_sAuthority = sqlr.GetString((int) iColumns.iAuthority);
                m_dttmCreateDate = sqlr.GetDateTime((int) iColumns.iCreateDate);
                m_sComment = sqlr.GetString((int) iColumns.iComment);
            }

            public static string s_sSqlQueryString =
                "SELECT " +
                "$$rwllcalendarlinks$$.LinkID, $$rwllcalendarlinks$$.TeamID, $$rwllcalendarlinks$$.Authority, " +
                "$$rwllcalendarlinks$$.CreateDate, $$rwllcalendarlinks$$.Comment" +
                "FROM $$#rwllpractice$$";

            public static Dictionary<string, string> s_mpAliases = new Dictionary<string, string>
            {
                {"rwllcalendarlinks", "RWP"},
            };

            public CalendarLinkItem()
            {
            }

            public static string s_sSqlInsert = "INSERT INTO rwllcalendarlinks (LinkID, TeamID, Authority, CreateDate, Comment) ";

            public static string s_sSqlDelete = "DELETE FROM rwllcalendarlinks WHERE LinkID='{0}'";

            /*----------------------------------------------------------------------------
            	%%Function: SGenerateUpdateQuery
            	%%Qualified: RwpApi.CalendarLinks.CalendarLinkItem.SGenerateUpdateQuery
            	%%Contact: rlittle
            	
            ----------------------------------------------------------------------------*/
            public string SGenerateUpdateQuery(TCore.Sql sql, bool fAdd)
            {
                if (!fAdd)
                    return null;
                string sValuesTemplate = "VALUES ('{0}','{1}','{2}','{3}','{4}')";

                string sQueryBase = s_sSqlInsert;
                string sQueryValues = String.Format(sValuesTemplate, m_guidLink.ToString(), Sql.Sqlify(m_sTeam),
                    Sql.Sqlify(m_sAuthority), m_dttmCreateDate.ToString("M/d/yyyy"), Sql.Sqlify(m_sComment));

                return String.Format("{0} {1}", sQueryBase, sQueryValues);
            }

            /*----------------------------------------------------------------------------
            	%%Function: CheckLength
            	%%Qualified: RwpApi.CalendarLinks.CalendarLinkItem.CheckLength
            	%%Contact: rlittle
            	
            ----------------------------------------------------------------------------*/
            static void CheckLength(string s, string sDesc, int nMax, List<string> plsFail)
            {
                if (s.Length >= nMax)
                    plsFail.Add(String.Format("{0} ({1}) is >= {2} characters", sDesc, s, nMax));
            }

            /*----------------------------------------------------------------------------
            	%%Function: FromCalendarLink
            	%%Qualified: RwpApi.CalendarLinks.CalendarLinkItem.FromCalendarLink
            	%%Contact: rlittle
            	
                convert a CalendarLink (from the wire) into our internal LinkItem format
            ----------------------------------------------------------------------------*/
            static CalendarLinkItem FromCalendarLink(CalendarLink linkItem)
            {
                return new CalendarLinkItem()
                {
                    m_guidLink = linkItem.Link, m_sTeam = linkItem.Team, m_sAuthority = linkItem.Authority,
                    m_dttmCreateDate = linkItem.CreateDate, m_sComment = linkItem.Comment
                };
            }

            /*----------------------------------------------------------------------------
            	%%Function: AddCalendarLinkItem
            	%%Qualified: RwpApi.CalendarLinks.CalendarLinkItem.AddCalendarLinkItem
            	%%Contact: rlittle
            	
            ----------------------------------------------------------------------------*/
            public static RSR AddCalendarLinkItem(CalendarLink link)
            {
                CalendarLinkItem item = FromCalendarLink(link);
                RSR sr = item.Preflight(null);

                if (!sr.Succeeded)
                    return sr;

                Sql sql;
                
                sr = RSR.FromSR(Sql.OpenConnection(out sql, Startup._sResourceConnString));
                if (!sr.Result)
                    return sr;

                sr = RSR.FromSR(sql.BeginTransaction());
                if (!sr.Result)
                {
                    sql.Close();
                    return sr;
                }

                try
                {
                    string sAdd = item.SGenerateUpdateQuery(sql, true);

                    SqlCommand sqlcmd = sql.CreateCommand();
                    sqlcmd.CommandText = sAdd;
                    sqlcmd.Transaction = sql.Transaction;
                    sqlcmd.ExecuteNonQuery();
                }
                catch (Exception exc)
                {
                    sql.Rollback();
                    sql.Close();
                    return RSR.Failed(exc);
                }

                sql.Commit();
                sql.Close();
                return RSR.Success();
            }

            /*----------------------------------------------------------------------------
            	%%Function: RevokeCalendarLink
            	%%Qualified: RwpApi.CalendarLinks.CalendarLinkItem.RevokeCalendarLink
            	%%Contact: rlittle
            	
            ----------------------------------------------------------------------------*/
            public static RSR RevokeCalendarLink(Guid guidLink)
            {
                RSR sr;

                if (guidLink == Guid.Empty)
                    return RSR.Failed("empty link id");

                Sql sql;
                
                sr = RSR.FromSR(Sql.OpenConnection(out sql, Startup._sResourceConnString));
                if (!sr.Result)
                    return sr;

                sr = RSR.FromSR(sql.BeginTransaction());
                if (!sr.Result)
                {
                    sql.Close();
                    return sr;
                }

                try
                {
                    string sDelete = String.Format(s_sSqlDelete, guidLink.ToString());

                    SqlCommand sqlcmd = sql.CreateCommand();
                    sqlcmd.CommandText = sDelete;
                    sqlcmd.Transaction = sql.Transaction;
                    sqlcmd.ExecuteNonQuery();
                }
                catch (Exception exc)
                {
                    sql.Rollback();
                    sql.Close();
                    return RSR.Failed(exc);
                }

                sql.Commit();
                sql.Close();
                return RSR.Success();
            }
            
            /*----------------------------------------------------------------------------
            	%%Function: SRFromPls
            	%%Qualified: RwpApi.CalendarLinks.CalendarLinkItem.SRFromPls
            	%%Contact: rlittle
            	
            ----------------------------------------------------------------------------*/
            public static RSR SRFromPls(string sReason, List<string> plsDiff)
            {
                string s = sReason;

                foreach (string sItem in plsDiff)
                    s += sItem + ", ";

                return RSR.Failed(s);
            }

            /*----------------------------------------------------------------------------
            	%%Function: Preflight
            	%%Qualified: RwpApi.CalendarLinks.CalendarLinkItem.Preflight
            	%%Contact: rlittle
            	
            ----------------------------------------------------------------------------*/
            public RSR Preflight(TCore.Sql sql)
            {
                List<string> plsFail = new List<string>();

                CheckLength(m_sComment, "Comment", 255, plsFail);

                if (plsFail.Count > 0)
                    return SRFromPls("preflight failed", plsFail);

                return RSR.Success();
            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: FAddResultRow
        	%%Qualified: RwpApi.CalendarLinks.FAddResultRow
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public bool FAddResultRow(SqlReader sqlr, int iRecordSet)
        {
            m_plcall.Add(new CalendarLinkItem(sqlr.Reader));
            return true;
        }
    }
}
