using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using NUnit.Framework;
using Syntactik.MonoDevelop.Highlighting;
using static SyntactikMDAddin.Tests.TestUtils;

namespace SyntactikMDAddin.Tests
{
    [TestFixture]
    public class SyntaxHighlightingTests : TextEditorTestBase
    {

        [Test, RecordedTest]
        public void Alias()
        {
            DoTest();
        }

        [Test, RecordedTest]
        public void Document()
        {
            DoTest();
        }

        [Test, RecordedTest]
        public void DQName()
        {
            DoTest();
        }

        [Test, RecordedTest]
        public void FreeOpenString()
        {
            DoTest();
        }

        [Test, RecordedTest]
        public void MultilineComment()
        {
            DoTest();
        }

        [Test, RecordedTest]
        public void OpenName()
        {
            DoTest();
        }

        [Test, RecordTest]
        public void OpenString()
        {
            DoTest();
        }

        [Test, RecordedTest]
        public void SQName()
        {
            DoTest();
        }

        [Test, RecordedTest]
        public void Wsa()
        {
            DoTest();
        }

        [Test, RecordedTest]
        public void ValidateSyntaxModes()
        {
            Assert.IsTrue(SyntaxModeService.ValidateAllSyntaxModes());
        }

    }
}
