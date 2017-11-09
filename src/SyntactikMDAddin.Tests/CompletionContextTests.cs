#if DEBUG
using static SyntactikMDAddin.Tests.TestUtils;
using NUnit.Framework;

namespace SyntactikMDAddin.Tests
{

    [TestFixture]
    public class CompletionContextTests
    {
        [Test, RecordedTest]
        public void DedentEof()
        {
            DoCompletionContextTest();
        }
        [Test, RecordedTest]
        public void IndentEof()
        {
            DoCompletionContextTest();
        }
        [Test, RecordedTest]
        public void IndentEofOffset()
        {
            DoCompletionContextTest(1);
        }
    }

}
#endif
