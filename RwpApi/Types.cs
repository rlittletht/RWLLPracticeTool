using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace RwpApi
{
	public class RSRBase //   : TCore.SRBase	// StatusResponse XML
	{
        public bool Result { get { return fResult; } set { fResult = value; } }
		public string Reason { get { return sReason;} set { sReason = value;}}
		public bool Succeeded { get { return fResult;}}

        bool fResult;
        string sReason;
	}

    public class RSR : RSRBase
    {
        static public RSR FromSR(TCore.SR sr)
        {
            RSR rsr = new RSR();

            rsr.Result = sr.Result;
            rsr.Reason = sr.Reason;

            return rsr;
        }

        static public RSR Success()
        {
            RSR sr = new RSR();
            sr.Result = true;
            sr.Reason = null;

            return sr;
        }

        static public RSR Failed(Exception e)
        {
            RSR sr = new RSR();
            sr.Result = false;
            sr.Reason = e.Message;

            return sr;
        }

        static public RSR Failed(string sReason)
        {
            RSR sr = new RSR();
            sr.Result = false;
            sr.Reason = sReason;

            return sr;
        }
    }

    public class TRSR<T> : RSRBase
    {
        private T m_t;

        public T TheValue { get { return m_t; } set { m_t = value; } }

        public static RSR ToRsr(TRSR<T> sr)
        {
            RSR rsr = new RSR();

            rsr.Reason = sr.Reason;
            rsr.Result = sr.Result;

            return rsr;
        }

        public static TRSR<T> FromSR(TCore.SR sr)
        {
            TRSR<T> vsr = new TRSR<T>();

            vsr.Result = sr.Result;
            vsr.Reason = sr.Reason;

            return vsr;
        }

        public static TRSR<T> Success()
        {
            TRSR<T> sr = new TRSR<T>();
            sr.Result = true;
            sr.Reason = null;

            return sr;
        }

        public static TRSR<T> Failed(Exception e)
        {
            TRSR<T> sr = new TRSR<T>();
            sr.Result = false;
            sr.Reason = e.Message;

            return sr;
        }

        public static TRSR<T> Failed(string sReason)
        {
            TRSR<T> sr = new TRSR<T>();
            sr.Result = false;
            sr.Reason = sReason;

            return sr;
        }
    }

    public class CalItem
    {
        string m_sTitle;
        DateTime m_dttmStart;
        DateTime m_dttmEnd;
        string m_sLocation;
        string m_sDescription;
        private string m_uid;

        public string Title { get { return m_sTitle; } set { m_sTitle = value; } }
        public DateTime Start { get { return m_dttmStart; } set { m_dttmStart = value; } }
        public DateTime End { get { return m_dttmEnd; } set { m_dttmEnd = value; } }
        public string Location { get { return m_sLocation; } set { m_sLocation = value; } }
        public string Description { get { return m_sDescription; } set { m_sDescription = value; } }
        public string UID { get { return m_uid; }set { m_uid = value; } }
    }

    public class RSR_CalItems : TRSR<List<CalItem>>
    {
        public static RSR_CalItems FromRSR(RSR rsr)
        {
            RSR_CalItems _twsr = new RSR_CalItems();

            _twsr.Reason = rsr.Reason;
            _twsr.Result = rsr.Result;

            return _twsr;
        }
    }

    [DataContract]
	public class SRXML : RSRBase	// StatusResponse XML
	{
		string sXML;
		DateTime dttm;

		//[DataMember]
		public string XML { get { return sXML;} set { sXML = value;}}

		//[DataMember]
		public DateTime Dttm { get { return dttm;} set { dttm = value;}}

		public SRXML(RSR sr)
		{
			sXML = null;
			dttm = DateTime.Now;

			Reason = sr.Reason;
			Result = sr.Result;
		}
	}

    public class ServerInfo
    {
        public string sSqlServerHash;
        public string sServerName;
    }

	[Serializable]
	public class RwpErrorSerial
	{
		public bool fSuccess;
		public string sReason;
	}

}

