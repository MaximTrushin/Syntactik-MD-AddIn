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

        [Test]
        public void TestSpans()
        {
            TestOutput("/* TestMe */",
                        "<span foreground=\"#888A85\">/* TestMe */</span>");
        }


        static void TestOutput(string input, string expectedMarkup)
        {
            string markup = GetMarkup(input);
            SaveMarkup(markup);

            if (markup != expectedMarkup)
            {
                Console.WriteLine("----Expected:");
                Console.WriteLine(expectedMarkup);
                Console.WriteLine("----Got:");
                Console.WriteLine(markup);
            }
            Assert.AreEqual(expectedMarkup, markup, "expected:" + expectedMarkup + Environment.NewLine + "But got:" + markup);
        }

        private static void SaveMarkup(string markup)
        {
            Directory.CreateDirectory(Path.Combine(AssemblyDirectory, "markup"));
            File.WriteAllText(Path.Combine(AssemblyDirectory,"markup", GetTestCaseName() + ".html"), markup);
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
