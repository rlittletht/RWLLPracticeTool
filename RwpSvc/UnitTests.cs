
using System;

namespace RwpSvc
{
    using NUnit.Framework;

    [TestFixture]
    public class TeamUnitTests
    {
        [Test]
        [TestCase("BB Majors Bob Walsh", "bowa")]
        [TestCase("SB Minors Bo Walsh", "bowa")]
        [TestCase("SB AA Bob Wi", "bowi")]
        [TestCase("SB A Bo Wi", "bowi")]
        [TestCase("SB A Bo", "")]
        [TestCase("SB A Bo Bo Fet", "bobo")]
        public void TestPasswordGen(string sTeamName, string sRootExpected)
        {
            string sPW = Practice.Teams.RwpTeam.SGenRandomPassword(new Random(), sTeamName);

            Assert.AreEqual(sRootExpected, sPW.Substring(0, sRootExpected.Length));
            Assert.AreEqual(8, sPW.Length);
        }
    }
    
}
