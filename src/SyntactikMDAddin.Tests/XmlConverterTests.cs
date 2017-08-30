using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using static SyntactikMDAddin.Tests.TestUtils;

namespace SyntactikMDAddin.Tests
{
    [TestFixture()]
    public class XmlConverterTests
    {
        [Test, RecordTest]
        public void Fragment1()
        {
            DoXmlConverterTest();
        }
    }
}
