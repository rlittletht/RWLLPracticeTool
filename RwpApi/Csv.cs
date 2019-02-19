using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RwpApi
{
    // ================================================================================
    // C S V 
    //
    // Base contracts:
    // 
    // SetStaticHeader - Remember the column headings (fields).  For most clients this
    // 					 is enough (though some clients like twuser.cs have dynamic
    //                   headers and override some of the header functions).  These
    //                   headers will be used as the keys into collections of values
    // 					 when making a CSV line as well as mapping indexed columns into
    //                   named mappings when opening a CSV
    // 
    // ReadHeaderFromCsv - Parse a CSV header, build up mappings from the CSV file into
    //                     our static headings
    // 
    // Header			- get a CSV header line suitable for writing to a CSV file
    // 
    // CsvMake    		- get a CSV line that represents the collection of values passed
    // 					  in 
    // ================================================================================
    public class Csv
    {
        protected static string CsvHeader;
        protected string[] m_rgsStaticHeader;


        protected List<string> m_plsHeadings = null;

        protected Dictionary<string, string>
            m_mpHeadingsMatch =
                null; // the keys are all in caps (for matching), and the values are the real casing in m_plsHeadings

        public Csv()
        {
            m_plsHeadings = new List<string>();
            m_mpHeadingsMatch = new Dictionary<string, string>();
        }

        /* S E T  S T A T I C  H E A D E R */
        /*----------------------------------------------------------------------------
            %%Function: SetStaticHeader
            %%Qualified: RwpSvc.Practice:Csv.SetStaticHeader
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        public void SetStaticHeader(string[] rgsStaticHeader)
        {
            m_rgsStaticHeader = rgsStaticHeader;

            foreach (string s in m_rgsStaticHeader)
            {
                m_plsHeadings.Add(s);
                m_mpHeadingsMatch.Add(s.ToUpper(), s);
            }
        }

        /* L I N E  T O  A R R A Y */
        /*----------------------------------------------------------------------------
            %%Function: LineToArray
            %%Qualified: RwpSvc.Practice:Csv.LineToArray
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        public static string[] LineToArray(string line)
        {
            String pattern = ",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))";
            Regex r = new Regex(pattern);

            string[] rgs = r.Split(line);

            for (int i = 0; i < rgs.Length; i++)
            {
                if (rgs[i].Length > 0 && rgs[i][0] == '"')
                    rgs[i] = rgs[i].Substring(1, rgs[i].Length - 2);
            }

            return rgs;
        }

        protected Dictionary<int, string> m_mpColHeader;
        protected Dictionary<string, int> m_mpHeaderCol;

        /* R E A D  H E A D E R  F R O M  S T R I N G */
        /*----------------------------------------------------------------------------
            %%Function: ReadHeaderFromString
            %%Qualified: tw.twsvc:TwUser:Csv.ReadHeaderFromString
            %%Contact: rlittle

            m_plsHeadings has the current set of headings that the database supports

            read in the heading line from the CSV file and validate what it wants
            to upload
        ----------------------------------------------------------------------------*/
        public RSR ReadHeaderFromString(string sLine)
        {
            string[] rgs = LineToArray(sLine);
            m_mpColHeader = new Dictionary<int, string>();
            m_mpHeaderCol = new Dictionary<string, int>();
            int i = 0;

            foreach (string s in rgs)
            {
                if (!m_mpHeadingsMatch.ContainsKey(s.ToUpper()))
                    return RSR.Failed(String.Format("header {0} not in database", s));

                m_mpColHeader.Add(i, s.ToUpper());
                m_mpHeaderCol.Add(s.ToUpper(), i);
                i++;
            }

            return RSR.Success();
        }

        /* G E T  S T R I N G  V A L */
        /*----------------------------------------------------------------------------
            %%Function: GetStringVal
            %%Qualified: tw.twsvc:TwUser.GetStringVal
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        protected string GetStringVal(string[] rgs, string s, string sCurValue, List<string> plsDiff)
        {
            string sNew;
            string sKey = s.ToUpper();
            if (m_mpHeaderCol.ContainsKey(sKey))
                sNew = rgs[m_mpHeaderCol[sKey]];
            else
                sNew = "";

            if (sNew != sCurValue)
                plsDiff.Add(String.Format("{0}('{1}' != '{2}')", s, sCurValue, sNew));

            return sNew;
        }

        /* G E T  S T R I N G  V A L */
        /*----------------------------------------------------------------------------
            %%Function: GetStringVal
            %%Qualified: tw.twsvc:TwUser.GetStringVal
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        protected string GetStringValNullable(string[] rgs, string s)
        {
            string sNew;
            string sKey = s.ToUpper();

            if (m_mpHeaderCol.ContainsKey(sKey))
                sNew = rgs[m_mpHeaderCol[sKey]];
            else
                sNew = null;

            return sNew;
        }

        /* G E T  S T R I N G  V A L */
        /*----------------------------------------------------------------------------
            %%Function: GetStringVal
            %%Qualified: RwpSvc.Practice:Csv.GetStringVal
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        protected string GetStringVal(string[] rgs, string s)
        {
            string sNew = GetStringValNullable(rgs, s);

            if (sNew == null)
                return "";

            return sNew;
        }

        /* G E T  D A T E  V A L  N U L L A B L E */
        /*----------------------------------------------------------------------------
            %%Function: GetDateValNullable
            %%Qualified: RwpSvc.Practice:Csv.GetDateValNullable
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        protected DateTime? GetDateValNullable(string[] rgs, string s)
        {
            string sNew;

            sNew = GetStringValNullable(rgs, s);
            if (sNew == null)
                return null;

            if (sNew.ToUpper().Contains("NULL"))
                sNew = "";

            if (sNew.Length == 0)
                return null;

            return DateTime.Parse(sNew);
        }

        /* G E T  D A T E  V A L */
        /*----------------------------------------------------------------------------
            %%Function: GetDateVal
            %%Qualified: RwpSvc.Practice:Csv.GetDateVal
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        protected DateTime GetDateVal(string[] rgs, string s)
        {
            DateTime? dttm = GetDateValNullable(rgs, s);

            if (dttm == null)
                throw new Exception(String.Format("cannot fetch date value for {0}", s));

            return dttm.Value;
        }

        /* G E T  I N T  V A L  N U L L A B L E */
        /*----------------------------------------------------------------------------
            %%Function: GetIntValNullable
            %%Qualified: RwpSvc.Practice:Csv.GetIntValNullable
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        protected int? GetIntValNullable(string[] rgs, string s)
        {
            string sNew;

            sNew = GetStringValNullable(rgs, s);
            if (sNew == null)
                return null;

            if (sNew.ToUpper().Contains("NULL"))
                sNew = "";

            if (sNew.Length == 0)
                return null;

            return Int32.Parse(sNew);
        }

        /* G E T  I N T  V A L */
        /*----------------------------------------------------------------------------
            %%Function: GetIntVal
            %%Qualified: RwpSvc.Practice:Csv.GetIntVal
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        protected int GetIntVal(string[] rgs, string s)
        {
            int? i = GetIntValNullable(rgs, s);

            if (i == null)
                return 0;

            return i.Value;
        }


        /* G E T  F L O A T  V A L  N U L L A B L E */
        /*----------------------------------------------------------------------------
            %%Function: GetDoubleValNullable
            %%Qualified: RwpSvc.Practice:Csv.GetDoubleValNullable
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        protected double? GetDoubleValNullable(string[] rgs, string s)
        {
            string sNew;

            sNew = GetStringValNullable(rgs, s);
            if (sNew == null)
                return null;

            if (sNew.ToUpper().Contains("NULL"))
                sNew = "";

            if (sNew.Length == 0)
                return null;

            return double.Parse(sNew);
        }


        /* G E T  F L O A T  V A L */
        /*----------------------------------------------------------------------------
            %%Function: GetDoubleVal
            %%Qualified: RwpSvc.Practice:Csv.GetDoubleVal
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        protected double GetDoubleVal(string[] rgs, string s)
        {
            double? fl = GetDoubleValNullable(rgs, s);

            if (fl == null)
                return 0.0f;

            return fl.Value;
        }


        /* C S V  M A K E */
        /*----------------------------------------------------------------------------
            %%Function: CsvMake
            %%Qualified: RwpSvc.Practice:Csv.CsvMake
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        protected string CsvMake(Dictionary<string, string> mpColData)
        {
            string sCsv = null;
            const string sTemplateFirst = "\"{0}\"";
            const string sTemplateNext = ",\"{0}\"";

            foreach (string sHeading in m_plsHeadings)
            {
                string s;

                if (mpColData.ContainsKey(sHeading))
                    s = mpColData[sHeading];
                else
                    s = "";

                if (sCsv == null)
                    sCsv = String.Format(sTemplateFirst, s);
                else
                    sCsv += String.Format(sTemplateNext, s);
            }

            return sCsv;
        }

        /* H E A D E R */
        /*----------------------------------------------------------------------------
            %%Function: Header
            %%Qualified: RwpSvc.Practice:Csv.Header
            %%Contact: rlittle

        ----------------------------------------------------------------------------*/
        public string Header()
        {
            Dictionary<string, string> mpColData = new Dictionary<string, string>();

            foreach (string s in m_plsHeadings)
                mpColData.Add(s, s);

            return CsvMake(mpColData);
        }

    }
}
