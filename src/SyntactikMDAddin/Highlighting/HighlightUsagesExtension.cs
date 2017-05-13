using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.TextEditor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.FindInFiles;

namespace Syntactik.MonoDevelop.Highlighting
{
    public class HighlightUsagesExtension : AbstractUsagesExtension<object>
    {
        protected override void Initialize()
        {
            base.Initialize();

            var textEditorData = DocumentContext.GetContent<TextEditorData>();
            if (textEditorData != null)
            {
                var syntaxMode = new SyntactikSyntaxMode(textEditorData.Document);
                textEditorData.Document.MimeType = "text/x-syntactik4xml";
                textEditorData.Document.SyntaxMode = syntaxMode;
            }
        }

        protected override Task<object> ResolveAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override Task<IEnumerable<MemberReference>> GetReferencesAsync(object resolveResult, CancellationToken token)
        {
            throw new NotImplementedException();
        }

    }
}
