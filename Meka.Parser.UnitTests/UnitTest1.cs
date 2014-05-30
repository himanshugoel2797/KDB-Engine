using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Meka.Parser.UnitTests
{
    [TestClass]
    public class TokenizerStage1
    {
        [TestMethod]
        public void ComponentsCheck()
        {
            KDBParser parser = KDBParser.FromFile("KnowledgeDB.kdb");
            parser.Parse();
        }

    }
}
