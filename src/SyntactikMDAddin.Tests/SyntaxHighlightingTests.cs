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
        public void AliasDef()
        {
            DoHighlightingTest();
        }

        [Test, RecordedTest]
        public void Arguments()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void Arguments2()
        {
            DoHighlightingTest();
        }

        [Test, RecordedTest]
        public void Attribute()
        {
            DoHighlightingTest();
        }
        [Test, RecordedTest]
        public void CommentsInWsa()
        {
            DoHighlightingTest();
        }

        [Test, RecordedTest]
        public void Document()
        {
            DoHighlightingTest();
        }

        [Test, RecordedTest]
        public void DQName()
        {
            DoHighlightingTest();
        }

        [Test, RecordedTest]
        public void FreeOpenString()
        {
            DoHighlightingTest();
        }
        
        [Test, RecordedTest]
        public void MultilineStrings()
        {
            DoHighlightingTest();
        }


        [Test, RecordedTest]
        public void MultilineComment()
        {
            DoHighlightingTest();
        }
        
        [Test, RecordedTest]
        public void NsDefAndScope()
        {
            DoHighlightingTest();
        }


        [Test, RecordedTest]
        public void OpenName()
        {
            DoHighlightingTest();
        }

        [Test, RecordedTest]
        public void OpenString()
        {
            DoHighlightingTest();
        }
        
        [Test, RecordedTest]
        public void SQEscapes()
        {
            DoHighlightingTest();
        }


        [Test, RecordedTest]
        public void SQName()
        {
            DoHighlightingTest();
        }

        [Test, RecordedTest]
        public void Wsa()
        {
            DoHighlightingTest();
        }

        [Test, RecordedTest]
        public void ValidateSyntaxModes()
        {
            Assert.IsTrue(SyntaxModeService.ValidateAllSyntaxModes());
        }
    }
}
