using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace RwpSvc
{
	[DataContract]
	public class RSRBase //   : TCore.SRBase	// StatusResponse XML
	{
		[DataMember]
        public bool Result { get { return fResult; } set { fResult = value; } }

		[DataMember]
		public string Reason { get { return sReason;} set { sReason = value;}}

		public bool Succeeded { get { return fResult;}}

        bool fResult;
        string sReason;
	}

	[DataContract]
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

	[Serializable]
	public class RwpErrorSerial
	{
		public bool fSuccess;
		public string sReason;
	}

}

