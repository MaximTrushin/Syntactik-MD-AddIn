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
        [Test]
        public void ValidateSyntaxModes()
        {
            Assert.IsTrue(SyntaxModeService.ValidateAllSyntaxModes());
        }

        [Test, RecordedTest]
        public void OpenName()
        {
            DoTest();
        }

        [Test, RecordedTest]
        public void SQName()
        {
            DoTest();
        }

        [Test, RecordedTest]
        public void DQName()
        {
            DoTest();
        }

        public static string GetMarkup(string input)
        {
            var data = new TextEditorData(new TextDocument(input));
            var syntaxMode = new SyntactikSyntaxMode(data.Document);
            data.Document.MimeType = "text/x-syntactik4xml";
            data.Document.SyntaxMode = syntaxMode;
            var schema = ColorScheme.LoadFrom(File.OpenRead(Path.Combine(AssemblyDirectory, "SyntactikColorSchema.json")));
            SyntaxModeService.AddStyle(schema);
            data.ColorStyle = SyntaxModeService.GetColorStyle("Syntactik");
            return data.GetMarkup(0, data.Length, false).Replace("foreground=\"", "style=\"color:");
        }
    }
}
