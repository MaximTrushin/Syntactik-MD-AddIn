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
    [TestFixture, RecordedTest]
    public class CompletionExpectationsTests
    {
        [Test, RecordedTest]
        public void Alias1()
        {
            DoCompletionExpectationsTest();
        }

        [Test, RecordedTest]
        public void Alias2()
        {
            DoCompletionExpectationsTest();
        }

        [Test, RecordedTest]
        public void Alias3()
        {
            DoCompletionExpectationsTest();
        }

        [Test, RecordedTest]
        public void Alias4()
        {
            DoCompletionExpectationsTest();
        }

        [Test, RecordedTest]
        public void AliasWithArguments()
        {
            DoCompletionExpectationsTest();
        }

        [Test, RecordedTest]
        public void Element1()
        {
            DoCompletionExpectationsTest();
        }
        [Test, RecordedTest]
        public void Document1()
        {
            DoCompletionExpectationsTest();
        }
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
        [Test, RecordedTest]
        public void NamespaceDefinition1()
        {
            DoCompletionExpectationsTest();
        }

    }
}
