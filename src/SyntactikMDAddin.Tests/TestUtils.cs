using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects;
using NUnit.Framework;
using Syntactik.Compiler;
using Syntactik.Compiler.IO;
using Syntactik.Compiler.Pipelines;
using Syntactik.DOM;
using Syntactik.MonoDevelop.Completion;
using Syntactik.MonoDevelop.Highlighting;
using Syntactik.MonoDevelop.Projects;
using Syntactik.MonoDevelop.Schemas;
using CompilerParameters = Syntactik.Compiler.CompilerParameters;

namespace SyntactikMDAddin.Tests
{
    public class TestUtils
    {
        public static void DoHighlightingTest()
        {
            var input = PrintTestScenario();

            string markup = GetMarkup(input);

            if (IsRecordedTest() || IsRecordTest())
                CompareResultAndRecordedFiles(markup, IsRecordTest(), "html");
        }

        public static void DoCompletionExpectationsTest()
        {
            var input = PrintTestScenario();

            var compilerParameters = CreateCompilerParameters(GetTestCaseName(), input);
            var compiler = new SyntactikCompiler(compilerParameters);
            var compilerContext = compiler.Run();
            var module = compilerContext.CompileUnit.Modules[0];

            Func<Dictionary<string, AliasDefinition>> func = () =>
            {
                var aliasDefs = new Dictionary<string, AliasDefinition>();
                foreach (var moduleMember in module.Members)
                {
                    var aliasDef = moduleMember as AliasDefinition;
                    if (aliasDef != null && !aliasDefs.ContainsKey(aliasDef.Name))
                        aliasDefs.Add(aliasDef.Name, aliasDef);
                }
                return aliasDefs;
            };
            var context = CompletionContext.CreateCompletionContext(GetTestCaseName(), input, input.Length, func);
            context.CalculateExpectations();
            var expectation = string.Join("\r\n", context.Expectations);
            if (IsRecordedTest() || IsRecordTest())
                CompareResultAndRecordedFiles(expectation, IsRecordTest(), "exp");
        }


        public static void DoCompletionListTest()
        {
            var input = PrintTestScenario();
            string completionList = GetCompletionList(GetTestCaseName(), input, new FileProvider());
            if (IsRecordedTest() || IsRecordTest())
                CompareResultAndRecordedFiles(completionList, IsRecordTest(), "list");
        }

        private static string GetCompletionList(string fileName, string text, IProjectFilesProvider filesProvider)
        {
            var compilerParameters = CreateCompilerParameters(fileName, text);
            var compiler = new SyntactikCompiler(compilerParameters);
            var compilerContext = compiler.Run();
            var module = compilerContext.CompileUnit.Modules[0];

            Dictionary<string, AliasDefinition> aliasDefs = null;
            Func<Dictionary<string, AliasDefinition>> func = () =>
            {
                if (aliasDefs != null) return aliasDefs;
                aliasDefs = new Dictionary<string, AliasDefinition>();
                foreach (var moduleMember in module.Members)
                {
                    var aliasDef = moduleMember as AliasDefinition;
                    if (aliasDef != null && !aliasDefs.ContainsKey(aliasDef.Name))
                        aliasDefs.Add(aliasDef.Name, aliasDef);
                }
                return aliasDefs;
            };

            CompletionContext context = CompletionContext.CreateCompletionContext(fileName, text, text.Length, func);
            context.CalculateExpectations();
            var lines = text.Split();
            var lastLine = lines[lines.Length - 1];
            var codeCompletionContext = new CodeCompletionContext { TriggerLineOffset = lastLine.Length > 0 ? lastLine.Length - 1 : 0 };
            var schemasRepository = new SchemasRepository(filesProvider);
            var list = SyntactikCompletionTextEditorExtension.GetCompletionList(context, codeCompletionContext, 0, func, schemasRepository);
            return string.Join("\n", list.Select(item => item.CompletionText));
        }

        private static CompilerParameters CreateCompilerParameters(string fileName, string content)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompileToMemory() };
            compilerParameters.Input.Add(new StringInput(fileName, content));
            return compilerParameters;
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
        /// <param name="result"></param>
        /// <param name="record">If true then result file is recorded not compared.</param>
        /// <param name="extension">Extension of saved file.</param>
        private static void CompareResultAndRecordedFiles(string result, bool record, string extension)
        {
            var recordedDir = AssemblyDirectory + @"\Scenarios\" + GetTestClassName() + @"\Recorded\";
            var recordedFileName = recordedDir + GetTestCaseName() + "." + extension;
            if (record)
            {
                SaveTest(result, extension);
                Console.WriteLine(result);
            }
            else
            {
                Assert.IsTrue(Directory.Exists(recordedDir), "Directory {0} doesn't exist", recordedDir);

                //Equal number of files
                Console.WriteLine();

                result = result.Replace("\r\n", "\n");
                var recorded = File.ReadAllText(recordedFileName).Replace("\r\n", "\n");

                Console.WriteLine(result);
                Assert.AreEqual(recorded, result);
            }
        }

        public static void SaveTest(string result, string extension)
        {
            var recordedDir = AssemblyDirectory + @"\..\..\Scenarios\" + GetTestClassName() + @"\Recorded\";
            var fileName = recordedDir + GetTestCaseName() + "." + extension;
            Directory.CreateDirectory(recordedDir);
            File.WriteAllText(fileName, result);
        }


        private static string PrintTestScenario()
        {
            var testCaseName = GetTestCaseName();

            var fileName = new StringBuilder(AssemblyDirectory + @"\Scenarios\").Append(GetTestClassName() + "\\"). Append(testCaseName).Append(".s4x").ToString();

            Console.WriteLine();
            Console.WriteLine(Path.GetFileName(fileName));
            var code = File.ReadAllText(fileName);
            PrintCode(code);
            return code;
        }


        public static void PrintCode(string code)
        {
            int line = 1;
            Console.WriteLine(@"Code:");
            Console.Write(@" ({0})", line);
            int offset = 0;
            foreach (var c in code)
            {
                if (c == '\r') continue;
                if (c == '\n')
                {
                    Console.Write(@" ({0})", offset);
                }

                Console.Write(c);
                offset++;
                if (c == '\n')
                {
                    line++;
                    Console.Write(@" ({0})", line);
                }
            }
            Console.Write(@" ({0})", offset);
            Console.WriteLine();
        }

        public static string AssemblyDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static string GetTestCaseName()
        {
            var trace = new StackTrace();
            return trace.GetFrames().Select(f => f.GetMethod()).First(m => m.CustomAttributes.Any(a => a.AttributeType.FullName == "NUnit.Framework.TestAttribute")).Name;
        }

        private static string GetTestClassName()
        {
            var trace = new StackTrace();
            var method =
                trace.GetFrames()
                    .Select(f => f.GetMethod())
                    .First(m => m.CustomAttributes.Any(a => a.AttributeType.FullName == "NUnit.Framework.TestAttribute"));
            var name = method.DeclaringType.Name;
            var result = name.Substring(0, name.Length - "Tests".Length);
            return result;
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

        public class FileProvider : IProjectFilesProvider
        {
            public IEnumerable<string> GetSchemaProjectFiles()
            {
                var files = new List<string>();
                var path = AssemblyDirectory + @"\Schemas";
                foreach (var file in Directory.GetFiles(path))
                {
                    files.Add(file);
                }
                return files;
            }
        }
    }
}
