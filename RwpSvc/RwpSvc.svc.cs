using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace RwpSvc
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Practice" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Practice.svc or Practice.svc.cs at the Solution Explorer and start debugging.
    [ServiceContract(Namespace = "")]
    public partial class Practice : System.Web.Services.WebService
    {
#if PRODDATA
        static string _sResourceConnString
        {
            get { return ConfigurationManager.AppSettings["Thetasoft.Azure.ConnectionString"]; }
        }
#elif STAGEDATA
        static string _sResourceConnString
        {
            get { return ConfigurationManager.AppSettings["Thetasoft.Staging.Azure.ConnectionString"]; }
        }
#else
    static string _sResourceConnString =
        "Server=cantorix;Database=db0902;Trusted_Connection=True;";
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
			return RwpSlots.ClearAll();
		}

		[OperationContract]
		[WebGet]
		public RSR ClearYear(int nYear)
		{
			return RwpSlots.ClearYear(nYear);
		}

        [OperationContract]
        [WebGet]
        public Stream GetCsvSlots()
        {
            Stream stm = new MemoryStream(4096);

            RwpSlots slots = new RwpSlots();

            slots.GetCsv(stm);
            stm.Flush();
            stm.Seek(0, SeekOrigin.Begin);
            return stm;
        }

        [OperationContract]
        [WebGet]
        public RSR_CalItems GetCalendarForTeam(string sTeamName)
        {
            return RwpSlots.GetCalendarItemsForTeam(sTeamName);
        }

        [OperationContract]
		[WebGet]
		public RSR ImportCsvSlots(Stream stmCsv)
		{
            return RwpSlots.ImportCsv(stmCsv);
		}

        static string ExtractServerNameFromConnection(string sConnection)
        {
            Regex rex = new Regex(".*server[ \t]*=[ \t]*([^;]*)[ \t]*;.*", RegexOptions.IgnoreCase);

            Match m = rex.Match(sConnection);
       
            return m.Groups[1].Value;
        }

        [TestCase("Server=foo;", "foo")]
        [TestCase("server=tcp:foo.bar.com; initial catalog=barfoo;uid=someone@nowhere.com;pwd=pass.word", "tcp:foo.bar.com")]
        [Test]
        public static void TestExtractServerNameFromConnection(string sIn, string sExpected)
        {
            Assert.AreEqual(sExpected.ToUpper(), ExtractServerNameFromConnection(sIn)?.ToUpper());
        }
        [OperationContract]
        [WebGet]
        public ServerInfo GetServerInfo()
        {
            ServerInfo si = new ServerInfo();

            si.sServerName = System.Net.Dns.GetHostName();

            si.sSqlServerHash = ExtractServerNameFromConnection(_sResourceConnString);
            return si;
        }
    }
}
