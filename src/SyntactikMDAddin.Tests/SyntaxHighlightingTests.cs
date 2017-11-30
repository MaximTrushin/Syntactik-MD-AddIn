#if DEBUG
using Mono.TextEditor.Highlighting;
using NUnit.Framework;
using static SyntactikMDAddin.Tests.TestUtils;

namespace SyntactikMDAddin.Tests
{
    [TestFixture]
    public class SyntaxHighlightingTests
    {

        [Test, RecordedTest]
        public void Alias()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void AliasJson()
        {
            DoJsonHighlightingTest();
        }

        [Test, RecordedTest]
        public void AliasDef()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void AliasDefJson()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void Arguments()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void ArgumentsJson()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void Arguments2()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void ArgumentsJson2()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void Attribute()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void AttributeJson()
        {
            DoJsonHighlightingTest();
        }

        [Test, RecordedTest]
        public void AttributeNoDelimiter()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void AttributeNoDelimiterJson()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void CommentsInWsa()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void CommentsInWsaJson()
        {
            DoJsonHighlightingTest();
        }

        [Test, RecordedTest]
        public void Document()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void DocumentJson()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void DQName()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void DQNameJson()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void DQValue()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void DQValueJson()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void FreeOpenString()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void FreeOpenStringJson()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void InlineOpenString()
        {
            DoHighlightingTest();
        }

        [Test, RecordedTest]
        public void InlineQuotedString()
        {
            DoHighlightingTest();
        }

        [Test, RecordedTest]
        public void JsonLiterals()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void MultilineComment()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void MultilineCommentJson()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void MultilineStrings()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void MultilineStringsJson()
        {
            DoJsonHighlightingTest();
        }

        [Test, RecordedTest]
        public void NsDefAndScope()
        {
            DoHighlightingTest();
        }

        [Test, RecordedTest]
        public void NsDefAndScopeJson()
        {
            DoJsonHighlightingTest();
        }

        [Test, RecordedTest]
        public void OpenName()
        {
            DoHighlightingTest();
        }

        [Test, RecordedTest]
        public void OpenNameJson()
        {
            DoJsonHighlightingTest();
        }

        [Test, RecordedTest]
        public void OpenString()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void OpenStringComments()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void OpenStringJson()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void PairValueWithParameters()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void DQEscapes()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void DQEscapesJson()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void SQName()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void SQNameJson()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void SQValue()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void SQValueJson()
        {
            DoJsonHighlightingTest();
        }
        [Test, RecordedTest]
        public void ValueChoice()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void Wsa()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void WsaJson()
        {
            DoJsonHighlightingTest();
        }

        [Test, RecordedTest]
        public void ValidateSyntaxModes()
        {
            Assert.IsTrue(SyntaxModeService.ValidateAllSyntaxModes());
        }
    }
}
#endif