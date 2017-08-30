using NUnit.Framework;
using static SyntactikMDAddin.Tests.TestUtils;

namespace SyntactikMDAddin.Tests
{
    [TestFixture]
    public class XmlConverterTests
    {
        [Test, RecordedTest]
        public void Fragment1()
        {
            DoXmlConverterTest();
        }
        [Test, RecordedTest]
        public void NamespaceResolution1()
        {
            DoXmlConverterTest();
        }
    }
}
