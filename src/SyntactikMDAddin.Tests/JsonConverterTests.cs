#if DEBUG
using NUnit.Framework;
using static SyntactikMDAddin.Tests.TestUtils;

namespace SyntactikMDAddin.Tests
{
    [TestFixture]
    public class JsonConverterTests
    {
        [Test, RecordedTest]
        public void PurchaseOrder()
        {
            DoJsonConverterTest();
        }
        [Test, RecordedTest]
        public void LiteralArray1()
        {
            DoJsonConverterTest();
        }
        [Test, RecordedTest]
        public void QuotedString1()
        {
            DoJsonConverterTest();
        }
        [Test, RecordedTest]
        public void QuotedName1()
        {
            DoJsonConverterTest();
        }
        [Test, RecordedTest]
        public void Literals()
        {
            DoJsonConverterTest();
        }
    }
}
#endif