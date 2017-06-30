using NUnit.Framework;
using static SyntactikMDAddin.Tests.TestUtils;

namespace SyntactikMDAddin.Tests
{
    [TestFixture]
    public class CompletionListTests
    {
        [Test, RecordedTest]
        public void Alias1()
        {
            DoCompletionListTest();
        }
        [Test, RecordedTest]
        public void Alias2()
        {
            DoCompletionListTest();
        }
        [Test, RecordedTest]
        public void AliasComplex1()
        {
            DoCompletionListTest();
        }
        [Test, RecordedTest]
        public void AliasComplex2()
        {
            DoCompletionListTest();
        }
        [Test, RecordedTest]
        public void AliasWithArguments1()
        {
            DoCompletionListTest();
        }
        
    }
}
