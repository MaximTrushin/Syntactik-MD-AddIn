using System.Collections.Specialized;
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
        public void Fragment2()
        {
            DoXmlConverterTest(null, 1, insertNewLine: true);
        }
        [Test, RecordedTest]
        public void NamespaceResolution1()
        {
            DoXmlConverterTest();
        }
        [Test, RecordedTest]
        public void NamespaceResolution2()
        {
            var declaredNamespaces = new ListDictionary { { "ipo", "http://www.example.com/IPO"} };
            DoXmlConverterTest(declaredNamespaces);
        }
    }
}
