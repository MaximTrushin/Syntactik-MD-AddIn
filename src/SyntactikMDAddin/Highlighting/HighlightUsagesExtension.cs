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


            //var sourceEditor = DocumentContext.GetContent<MonoDevelop.SourceEditorView>();

            ////var syntaxMode = new MalinaSyntaxMode(Document.Editor.Document);
            //var syntaxMode = new MalinaLexerSyntaxMode(Document.Editor.Document);
            //TextEditorData.Document.MimeType = "text/x-malina";
            //TextEditorData.Document.SyntaxMode = syntaxMode;


            //var textEditor = DocumentContext..Editor.Parent as ExtensibleTextEditor;
            //var sourceEditor = Document.GetContent<SourceEditorView>();
            //if (sourceEditor != null && sourceEditor.Project != null)
            //{
            //    var opt = textEditor.TextArea.Options as ISourceEditorOptions;
            //    var mOpt = new MalinaOptions(opt);
            //    mOpt.ColorScheme = "Tango";
            //    textEditor.TextArea.Options = mOpt;
            //    textEditor.TextArea.TextViewMargin.PurgeLayoutCache();
            //}

        }

        //private SyntactikSyntaxMode syntaxMode;
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
