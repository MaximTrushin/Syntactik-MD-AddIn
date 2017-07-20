using System;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.TypeSystem;
using Syntactik.MonoDevelop.Projects;
using TS = MonoDevelop.Ide.TypeSystem;

namespace Syntactik.MonoDevelop.Parser
{
    public sealed class TypeSystemParser: TS.TypeSystemParser
    {
        public override async Task<ParsedDocument> Parse(ParseOptions options, CancellationToken cancellationToken = new CancellationToken())
        {
            var fileName = options.FileName;
            var project = (SyntactikProject)options.Project;
            ParsedDocument result;

            //Parse if document has newer version
            if (options.OldParsedDocument == null ||
                !((SyntactikParsedDocument)options.OldParsedDocument).ContentVersion.BelongsToSameDocumentAs(options.Content.Version) ||
                ((SyntactikParsedDocument)options.OldParsedDocument).ContentVersion.CompareAge(options.Content.Version) != 0
                
                )
            {
                result = await project.ParseSyntactikDocument(options.FileName, options.Content.Text, options.Content.Version, cancellationToken);
            }
            else
            {
                result = options.OldParsedDocument;
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
            return result;
        }
    }
}
