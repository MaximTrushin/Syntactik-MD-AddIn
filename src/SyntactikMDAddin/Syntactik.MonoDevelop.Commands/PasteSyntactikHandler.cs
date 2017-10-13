using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gtk;
using Mono.TextEditor;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Content;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.MonoDevelop.Completion;
using Syntactik.MonoDevelop.Projects;
using Module = Syntactik.DOM.Module;

namespace Syntactik.MonoDevelop.Commands
{
    internal class PasteSyntactikHandler : CommandHandler
    {
        protected override void Run()
        {
            var textEditor = IdeApp.Workbench.ActiveDocument?.Editor;
            if (textEditor == null) return;

            var document = IdeApp.Workbench.ActiveDocument.Window.ActiveViewContent.WorkbenchWindow.Document; //doing this to get right document in case of SynactikView 
            var data = document.GetContent<TextEditorData>();
            if (!data.CanEdit(data.Document.OffsetToLineNumber(data.IsSomethingSelected ? data.SelectionRange.Offset : data.Caret.Offset)))
                return;


            var project = document.Project as SyntactikProject;
            var module = document?.ParsedDocument?.Ast as Module;
            if (module == null || project == null) return;

            bool insertNewLine = false;
            var indentLevel = 0;
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
                PasteXmlHandler.GetIndentInfo(module, textEditor, out indentChar, out indentMultiplicity);
                var lastPair = context.LastPair as IMappedPair;
                

                var caretLine = textEditor.CaretLine;
                if (lastPair == null)
                {
                    //module

                }
                else if (lastPair.ValueInterval != null) //TODO: Create unit tests
                {
                    if (caretLine == lastPair.ValueInterval.End.Line)
                    {
                        insertNewLine = true;
                    }
                    indentLevel = lastPair.ValueIndent / indentMultiplicity - 1;
                }
                else if (caretLine == lastPair.NameInterval.End.Line)
                {
                    insertNewLine = true;
                    var pair = (Pair)lastPair;
                    indentLevel = lastPair.ValueIndent / indentMultiplicity - 1;
                    if (pair.Delimiter == DelimiterEnum.C || pair.Delimiter == DelimiterEnum.CC) indentLevel++;
                }
                else
                {
                    indentLevel = lastPair.ValueIndent / indentMultiplicity;
                    if (indentLevel == 0) indentLevel = 1; //indent = 0 means ValueIndent = 1 (default value) and indentMultiplicity > 1.
                }
            }
            var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            if (clipboard.WaitIsTargetAvailable(ClipboardActions.CopyOperation.MD_ATOM))
            {
                var lineText = textEditor.GetLineText(data.Document.OffsetToLineNumber(data.IsSomethingSelected ? data.SelectionRange.Offset : data.Caret.Offset));
                var firstLineindent = 0;
                if (!insertNewLine) firstLineindent = Math.Min(lineText.Length - lineText.TrimStart().Length, textEditor.CaretColumn - 1);


                clipboard.RequestContents(ClipboardActions.CopyOperation.MD_ATOM,
                    (clp, selectionData) =>
                    {
                        if (selectionData == null || selectionData.Length == 0) return;
                        byte[] copyData = selectionData.Data;
                        var originalfirstLineIndent = BitConverter.ToInt32(copyData, 2);
                        var originalIndentMultiplicity = BitConverter.ToInt32(copyData, 6);

                        string text = clipboard.WaitForText().TrimStart();
                        string s = Normalize(text, indentLevel, insertNewLine, indentChar, indentMultiplicity, firstLineindent,
                            originalfirstLineIndent, originalIndentMultiplicity);
                        using (textEditor.OpenUndoGroup())
                        {
                            textEditor.EnsureCaretIsNotVirtual();
                            textEditor.InsertAtCaret(s);
                        }
                        return;
                    });
            }
        }

        static readonly Regex rxNormalizeIndents = new Regex(@"((?<indent>[\t ]*)(?<text>[^\n\r]*(\r\n|\r|\n)?))");
        internal static string Normalize(string text, int indentLevel, bool addNewLine, char indentChar, int indentMultiplicity, 
            int firstLineIndent, int originalFirstLineIndent, int originalIndentMultiplicity)
        {
            var matches = rxNormalizeIndents.Matches(text).Cast<Match>().Where(m => m.Length > 0).Select(m => new { Indent = m.Groups["indent"].Value.Length, Text = m.Groups["text"].Value }).ToList();
            if (matches.Count > 0) matches.Insert(0, new { Indent = originalFirstLineIndent, matches[0].Text});
            matches.RemoveAt(1);
            var nonEmpty = matches.Where(m => !string.IsNullOrEmpty(m.Text.Trim())).ToList();
            var minOriginalIndentLevel = 0;
            if (nonEmpty.Any())
                minOriginalIndentLevel = nonEmpty.Min(m => m.Indent);
            if (minOriginalIndentLevel % originalIndentMultiplicity == 0)
                minOriginalIndentLevel = minOriginalIndentLevel / originalIndentMultiplicity;
            else
                minOriginalIndentLevel = minOriginalIndentLevel / originalIndentMultiplicity + 1; //Just in case indent is invalid

            var normalized = new StringBuilder();
            if (addNewLine) normalized.AppendLine();
            foreach (var l in matches)
            {
                int originalIndentLevel;
                if (l.Indent % originalIndentMultiplicity == 0)
                    originalIndentLevel = l.Indent / originalIndentMultiplicity;
                else
                    originalIndentLevel = l.Indent / originalIndentMultiplicity + 1; //Just in case indent is invalid

                var indentString = new string(indentChar, 
                        (indentLevel + originalIndentLevel - minOriginalIndentLevel) * indentMultiplicity - (l != matches[0] ? 0 : firstLineIndent)
                    ); //base indent
                normalized.AppendFormat("{0}{1}", indentString, l.Text);
            }
            var result = normalized.ToString();

            return result;
        }

        protected override void Update(CommandInfo info)
        {
            info.Enabled = false;
            info.Visible = false;
            var doc = IdeApp.Workbench.ActiveDocument;
            string extension;
            if (doc.FileName.Extension.ToLower() == ".xml")
            {
                var w = doc.Window;
                extension = w.ActiveViewContent.WorkbenchWindow.Document.FileName.Extension.ToLower();
            }
            else
            {
                extension = doc.FileName.Extension.ToLower();
            }
            if (extension == ".s4x" || extension == ".s4j")
            {
                info.Visible = true;
            }

            if (!info.Visible) return;
#if WIN32
            bool inWpf = false;
			if (System.Windows.Input.Keyboard.FocusedElement != null)
				inWpf = true;
#endif
            var handler = doc.GetContent<IClipboardHandler>();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var clipboard = Clipboard.Get(ClipboardActions.CopyOperation.CLIPBOARD_ATOM);
#if WIN32
            if (!inWpf && handler != null && handler.EnablePaste &&
#else
            if (handler != null && handler.EnablePaste &&
#endif
                clipboard.WaitIsTargetAvailable(ClipboardActions.CopyOperation.MD_ATOM))
                info.Enabled = true;
            else
                info.Bypass = true;
        }

        public string FormatPlainText(int offset, string text, byte[] copyData)
        {
            return text;
        }
    }
}
