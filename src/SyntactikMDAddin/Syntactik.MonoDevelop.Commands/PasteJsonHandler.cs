using System;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Content;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.MonoDevelop.Completion;
using Syntactik.MonoDevelop.Converter;
using Syntactik.MonoDevelop.Licensing;
using Syntactik.MonoDevelop.Projects;
using Module = Syntactik.DOM.Module;

namespace Syntactik.MonoDevelop.Commands
{
    internal class PasteJsonHandler : CommandHandler //TODO: Unit tests
    {
        protected override void Run()
        {
            var textEditor = IdeApp.Workbench.ActiveDocument?.Editor;
            if (textEditor == null) return;
            
            var document = IdeApp.Workbench.ActiveDocument;
            var project = document.Project as SyntactikProject;
            var module = document?.ParsedDocument?.Ast as Module;
            if (module == null || project == null) return;
            bool insertNewLine = false;
            var indent = 0;
            var indentChar = '\t';
            var indentMultiplicity = 1;
            var ext = textEditor.GetContent<SyntactikCompletionTextEditorExtension>();
            var task = ext.CompletionContextTask?.Task;
            if (task != null)
            {
#if DEBUG
                task.Wait(ext.CompletionContextTask.CancellationToken);
#else
                task.Wait(2000, ext.CompletionContextTask.CancellationToken);
#endif
                if (task.Status != TaskStatus.RanToCompletion) return;
                CompletionContext context = task.Result;
                GetIndentInfo(module, textEditor, out indentChar, out indentMultiplicity);
                var lastPair = context.LastPair as IMappedPair;
                if (lastPair == null) return;

                var caretLine = textEditor.CaretLine;

                if (lastPair.ValueInterval != null) //TODO: Create unit tests
                {
                    if (caretLine == lastPair.ValueInterval.End.Line)
                    {
                        insertNewLine = true;
                    }
                    indent = lastPair.ValueIndent / indentMultiplicity - 1;
                }
                else if (caretLine == lastPair.NameInterval.End.Line)
                {
                    insertNewLine = true;
                    var pair = (Pair) lastPair;
                    indent = lastPair.ValueIndent / indentMultiplicity - 1;
                    if (pair.Delimiter == DelimiterEnum.C || pair.Delimiter == DelimiterEnum.CC) indent++;
                }
                else
                {
                    indent = lastPair.ValueIndent / indentMultiplicity;
                }
            }
            var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            string text = clipboard.WaitForText().TrimStart();
            using (var monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor())
            {
                try
                {
                    string s4j;
                    var converter = new JsonToSyntactikConverter(text);

                    if (converter.Convert(indent, indentChar, indentMultiplicity, insertNewLine, out s4j))
                    {
                        using (textEditor.OpenUndoGroup())
                        {
                            textEditor.EnsureCaretIsNotVirtual();
                            textEditor.InsertAtCaret(s4j);
                        }
                    }
                    monitor.ReportSuccess(SuccessMessage);
                }
                catch (Exception)
                {
                    monitor.ReportError(ErrorMessage);
                }
            }
        }

        protected virtual string SuccessMessage => "Syntactik code is generated from JSON in clipboard.";
        protected virtual string ErrorMessage => "Text in clipboard is not valid JSON.";

        internal static void GetIndentInfo(Module module, TextEditor textEditor, out char indentSymbol,
            out int indentMultiplicity)
        {
            indentSymbol = module.IndentSymbol;
            indentMultiplicity = module.IndentMultiplicity;
            if (indentMultiplicity != 0) return;

            var text = textEditor.GetLineText(textEditor.CaretLine);
            if (string.IsNullOrEmpty(text))
            {
                indentMultiplicity = 1;
                return;
            }
            indentMultiplicity = text.Length - text.TrimStart().Length;
            indentSymbol = text[0];
            if (indentMultiplicity != 0) return;
            indentMultiplicity = 1;
            indentSymbol = '\t';
        }

        protected override void Update(CommandInfo info)
        {
            info.Enabled = false;
            var doc = IdeApp.Workbench.ActiveDocument;
            if (doc == null || ((SyntactikProject)doc.Project).License.RuntimeMode == Mode.Demo)
            {
                info.Bypass = true;
                return;
            }
            info.Visible = doc.FileName.Extension.ToLower() == ".s4j";

            if (!info.Visible) return;
            bool inWpf = false;
#if WIN32
			if (System.Windows.Input.Keyboard.FocusedElement != null)
				inWpf = true;
#endif
            var handler = doc.GetContent<IClipboardHandler>();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!inWpf && handler != null && handler.EnablePaste)
                info.Enabled = true;
            else
                info.Bypass = true;
        }
    }
}