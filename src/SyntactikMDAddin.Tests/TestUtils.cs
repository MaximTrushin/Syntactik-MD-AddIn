using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using NUnit.Framework;
using Syntactik.MonoDevelop.Highlighting;

namespace SyntactikMDAddin.Tests
{
    public class TestUtils
    {
        public static void DoTest()
        {
            var input = PrintTestScenario();

            string markup = GetMarkup(input);

            if (IsRecordedTest() || IsRecordTest())
                CompareResultAndRecordedFiles(markup, IsRecordTest());
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
            return "<span>&nbsp;</span>" + data.GetMarkup(0, data.Length, false).Replace("foreground=\"", "style=\"color:").Replace("\r\n", "<br><span>&nbsp;</span>").Replace("\n", "<br><span>&nbsp;</span>");
        }


        /// <summary>
        /// Record result file or compare it with previously recorded result
        /// </summary>
        /// <param name="markup"></param>
        /// <param name="record">If true then result file is recorded not compared.</param>
        private static void CompareResultAndRecordedFiles(string markup, bool record)
        {
            var testCaseName = GetTestCaseName();
            var recordedDir = AssemblyDirectory + @"\Scenarios\Recorded\";
            var recordedFileName = recordedDir + GetTestCaseName() + ".html";
            if (record)
            {
                SaveTest(markup);
            }
            else
            {
                Assert.IsTrue(Directory.Exists(recordedDir), "Directory {0} doesn't exist", recordedDir);

                //Equal number of files
                Console.WriteLine();

                var result = markup.Replace("\r\n", "\n");
                var recorded = File.ReadAllText(recordedFileName).Replace("\r\n", "\n");

                Console.WriteLine(result);
                Assert.AreEqual(recorded, result);
            }
        }

        public static void SaveTest(string result)
        {
            var testCaseName = GetTestCaseName();
            var recordedDir = AssemblyDirectory + @"\..\..\Scenarios\Recorded\";
            var fileName = recordedDir + GetTestCaseName() + ".html";
            Directory.CreateDirectory(recordedDir);
            File.WriteAllText(fileName, result);
        }


        private static string PrintTestScenario()
        {
            var testCaseName = GetTestCaseName();

            var fileName = new StringBuilder(AssemblyDirectory + @"\Scenarios\").Append(testCaseName).Append(".s4x").ToString();

            Console.WriteLine();
            Console.WriteLine(Path.GetFileName(fileName));
            var code = File.ReadAllText(fileName);
            PrintCode(code);
            return code;
        }


        public static void PrintCode(string code)
        {
            int line = 1;
            Console.WriteLine("Code:");
            Console.Write("{0}:\t ", line);
            int offset = 0;
            foreach (var c in code)
            {
                if (c == '\r') continue;
                if (c == '\n')
                {
                    Console.Write(" ({0})", offset);
                }

                Console.Write(c);
                offset++;
                if (c == '\n')
                {
                    line++;
                    Console.Write("{0}:\t ", line);
                }
            }
            Console.Write(" ({0})", offset);
            Console.WriteLine();
        }

        public static string AssemblyDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static string GetTestCaseName()
        {
            var trace = new StackTrace();
            return trace.GetFrames().Select(f => f.GetMethod()).First(m => m.CustomAttributes.Any(a => a.AttributeType.FullName == "NUnit.Framework.TestAttribute")).Name;
        }

        private static bool TestHasAttribute<T>()
        {
            var trace = new StackTrace();
            var method = trace.GetFrames().Select(f => f.GetMethod()).First(m => m.CustomAttributes.Any(a => a.AttributeType == typeof(TestAttribute)));
            Debug.Assert(method.DeclaringType != null, "method.DeclaringType != null");
            return method.CustomAttributes.Any(ca => ca.AttributeType == typeof(T)) ||
                method.DeclaringType.CustomAttributes.Any(ca => ca.AttributeType == typeof(T));
        }

        public static bool IsRecordedTest()
        {
            return TestHasAttribute<RecordedTestAttribute>();
        }

        public static bool IsRecordTest()
        {
            return TestHasAttribute<RecordTestAttribute>();
        }
    }
}
