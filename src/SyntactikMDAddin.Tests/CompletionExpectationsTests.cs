using NUnit.Framework;
using static SyntactikMDAddin.Tests.TestUtils;

namespace SyntactikMDAddin.Tests
{
    /// <summary>
    /// CompletionContext calculates what kind of nodes are expected at the current position:
    ///        Alias,
    ///        Element,
    ///        Argument,
    ///        Namespace,
    ///        Attribute,
    ///        Value,
    ///        Document,
    ///        AliasDefinition
    /// </summary>
    [TestFixture]
    public class CompletionExpectationsTests
    {
        [Test, RecordedTest]
        public void Module1()
        {
            DoCompletionExpectationsTest();
        }

        [Test, RecordedTest]
        public void Module2()
        {
            DoCompletionExpectationsTest();
        }

    }
}
