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
using DocumentRegion = MonoDevelop.Ide.Editor.DocumentRegion;
using TS = MonoDevelop.Ide.TypeSystem;

namespace Syntactik.MonoDevelop.Parser
{
    public sealed class TypeSystemParser: TS.TypeSystemParser
    {

        public override Task<ParsedDocument> Parse(ParseOptions options, CancellationToken cancellationToken = new CancellationToken())
        {
            var fileName = options.FileName;
            var project = (SyntactikProject)options.Project;
            DefaultParsedDocument result;

            //Parse if document has newer version
            if (options.OldParsedDocument == null ||
                !((SyntactikParsedDocument)options.OldParsedDocument).ContentVersion.BelongsToSameDocumentAs(options.Content.Version) ||
                ((SyntactikParsedDocument)options.OldParsedDocument).ContentVersion.CompareAge(options.Content.Version) != 0
                
                )
            {
                result = project.ParseSyntactikDocument(options.FileName, options.Content.Text, options.Content.Version, cancellationToken);
            }
            else
            {
                result = (DefaultParsedDocument)options.OldParsedDocument;
            }

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
            return Task.FromResult((ParsedDocument)result);
        }


    }
}
