using System;
using System.Threading;
using System.Threading.Tasks;
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;
using Syntactik.Compiler;
using Syntactik.Compiler.IO;
using Syntactik.Compiler.Pipelines;
using Syntactik.Compiler.Steps;
using TS = MonoDevelop.Ide.TypeSystem;

namespace Syntactik.MonoDevelop.Parser
{
    public sealed class TypeSystemParser: TS.TypeSystemParser
    {

        public override Task<ParsedDocument> Parse(ParseOptions options, CancellationToken cancellationToken = new CancellationToken())
        {
            var fileName = options.FileName;
            var project = options.Project;
            ParsedDocument result = ParseSyntactikDocument(options, cancellationToken);

            DateTime time;
            try
            {
                time = System.IO.File.GetLastWriteTimeUtc(fileName);
            }
            catch (Exception)
            {
                time = DateTime.UtcNow;
            }
            result.LastWriteTimeUtc = time;
            return Task.FromResult(result);
        }

        private SyntactikParsedDocument ParseSyntactikDocument(ParseOptions options, CancellationToken cancellationToken)
        {
            var result = new SyntactikParsedDocument(options.FileName);
            var compilerParameters = CreateCompilerParameters(options, result, cancellationToken);
            var compiler = new SyntactikCompiler(compilerParameters);
            compiler.Run();
            return result;
        }

        private CompilerParameters CreateCompilerParameters(ParseOptions options, SyntactikParsedDocument result, CancellationToken cancellationToken)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new ParseAndCreateFolding(result, cancellationToken));
            compilerParameters.Pipeline.Steps.Add(new ProcessAliasesAndNamespaces());
            compilerParameters.Pipeline.Steps.Add(new ValidateDocuments());
            compilerParameters.Input.Add(new StringInput(options.FileName, options.Content.Text));
            return compilerParameters;
        }
    }
}
