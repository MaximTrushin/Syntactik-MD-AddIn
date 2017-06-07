using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mono.TextEditor;
using MonoDevelop.Ide.Editor;
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
                var textEditor = textEditorData.Parent;
                var opt = textEditor.TextArea.Options;

                Mono.TextEditor.Highlighting.ColorScheme scheme;
                using (var stream = new MemoryStream(Syntactik.MonoDevelop.Properties.Resources.SyntactikStyle))
                {
                    scheme = Mono.TextEditor.Highlighting.ColorScheme.LoadFrom(stream);

                }
                textEditorData.ColorStyle = scheme;

                opt.Changed += (sender, args) => { textEditorData.ColorStyle = scheme; };

                var syntaxMode = new SyntactikSyntaxMode(textEditorData.Document);
                textEditorData.Document.MimeType = "text/x-syntactik4xml";
                textEditorData.Document.SyntaxMode = syntaxMode;
            }
        }

        protected override Task<object> ResolveAsync(CancellationToken token)
        {
            return null;
        }

        protected override Task<IEnumerable<MemberReference>> GetReferencesAsync(object resolveResult, CancellationToken token)
        {
            return null;
        }

    }
}
