using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mono.TextEditor;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.Gui;
using Syntactik.MonoDevelop.DisplayBinding;

namespace Syntactik.MonoDevelop.Highlighting
{
    public class HighlightUsagesExtension : AbstractUsagesExtension<object>
    {
        protected override void Initialize()
        {
            base.Initialize();
            var document = (Document)DocumentContext;
            var textEditorData = DocumentContext.GetContent<TextEditorData>();
            
            if (textEditorData == null) return;
            var textEditor = textEditorData.Parent;

            if (document.FileName.Extension == ".xml")
            {
                var view = document.Window.ActiveViewContent;
                textEditorData = view.GetContent<TextEditorData>();
            }

            var opt = textEditor.TextArea.Options;

            Mono.TextEditor.Highlighting.ColorScheme scheme;
            using (var stream = new MemoryStream(Properties.Resources.SyntactikStyle))
            {
                scheme = Mono.TextEditor.Highlighting.ColorScheme.LoadFrom(stream);

            }
            textEditorData.ColorStyle = scheme;

            opt.Changed += (sender, args) => { textEditorData.ColorStyle = scheme; };

            var syntaxMode = new SyntactikSyntaxMode(textEditorData.Document);
            textEditorData.Document.SyntaxMode = syntaxMode;
        }

        protected override Task<object> ResolveAsync(CancellationToken token)
        {
            return Task.FromResult<object>(null);
        }

        protected override Task<IEnumerable<MemberReference>> GetReferencesAsync(object resolveResult, CancellationToken token)
        {
            return Task.FromResult<IEnumerable<MemberReference>>(null);
        }

        public override bool IsValidInContext(DocumentContext context)
        {
            return context.Name.EndsWith(".s4x") || context.Name.EndsWith(".s4j") ||  context is SyntactikDocument;
        }
    }
}
