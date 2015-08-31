using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace RwpSvc
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Practice" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Practice.svc or Practice.svc.cs at the Solution Explorer and start debugging.
    [ServiceContract(Namespace = "")]
    public partial class Practice : System.Web.Services.WebService
    {
#if PRODUCTION
#if AZURE
// This line contained a SECRET and was automatically sanitized. This file will probably not compile now. Contact original author for the secret line
#else
// This line contained a SECRET and was automatically sanitized. This file will probably not compile now. Contact original author for the secret line
#endif
#else
#if STAGING
    static string _sResourceConnString =
        "Server=cacofonix;Database=db0902;Trusted_Connection=True;";
#else
	    static string _sResourceConnString =
// This line contained a SECRET and was automatically sanitized. This file will probably not compile now. Contact original author for the secret line
#endif
    // static string _sResourceConnString = "Data Source=cacofonix;Database=db0902;Trusted_Connection=Yes";
#endif
        [OperationContract]
        [WebGet]
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        [OperationContract]
        [WebGet]
        public string GetCsvTeams()
        {
            Stream stm = new MemoryStream(4096);

            Teams teams = new Teams();

            teams.GetCsv(stm);
            stm.Flush();
            stm.Seek(0, SeekOrigin.Begin);

            StreamReader sr = new StreamReader(stm);

            return sr.ReadToEnd();
        }

		[OperationContract]
		[WebGet]
		public Stream GetCsvTeamsStream()
		{
    		Stream stm = new MemoryStream(4096);

			Teams teams = new Teams();

			teams.GetCsv(stm);
		    stm.Flush();
		    stm.Seek(0, SeekOrigin.Begin);
		    return stm;
		}

        [OperationContract]
        [WebGet]
        public RSR ImportCsvTeams(Stream stmCsv)
        {
            RSR sr;

            sr = Teams.ImportCsv(stmCsv);
            return sr;
        }

        [OperationContract]
        [WebGet]
        public RSR ClearTeams()
        {
            return Teams.ClearTeams();
        }

		[OperationContract]
		[WebGet]
		public RSR ClearSlots()
		{
			return Slots.ClearAll();
		}

		[OperationContract]
		[WebGet]
		public RSR ClearYear(int nYear)
		{
			return Slots.ClearYear(nYear);
		}

        [OperationContract]
        [WebGet]
        public Stream GetCsvSlots()
        {
            Stream stm = new MemoryStream(4096);

            Slots slots = new Slots();

            slots.GetCsv(stm);
            stm.Flush();
            stm.Seek(0, SeekOrigin.Begin);
            return stm;
        }

        [OperationContract]
		[WebGet]
		public RSR ImportCsvSlots(Stream stmCsv)
		{
            return Slots.ImportCsv(stmCsv);
		}
    }
}
