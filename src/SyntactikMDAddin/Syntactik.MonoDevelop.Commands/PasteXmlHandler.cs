using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.MonoDevelop.Completion;
using Syntactik.MonoDevelop.Converter;
using Syntactik.MonoDevelop.Projects;
using Module = Syntactik.DOM.Module;

namespace Syntactik.MonoDevelop.Commands
{
    internal class PasteXmlHandler : CommandHandler  //TODO: Unit tests
    {
        protected override void Run()
        {
            var textEditor = IdeApp.Workbench.ActiveDocument?.Editor;
            if (textEditor == null) return;
            textEditor.EnsureCaretIsNotVirtual();
            var document = IdeApp.Workbench.ActiveDocument;
            var project = document.Project as SyntactikProject;
            var module = document?.ParsedDocument?.Ast as Module;
            if (module == null || project == null) return;
            //var modules = new PairCollection<Module> { module };


            bool insertNewLine = false;
            var indent = 0;
            var indentChar = '\t';
            var indentMultiplicity = 1;

            //var pair = FindCurrentPairInModule(module, textEditor.CaretOffset);
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
                GetIndentInfo(context, textEditor, out indentChar, out indentMultiplicity);
                var lastPair = context.LastPair as IMappedPair;
                if (lastPair == null) return;

                var caretLine = textEditor.CaretLine;

                if (lastPair.ValueInterval != null)
                {
                    if (caretLine == lastPair.ValueInterval.End.Line)
                    {
                        insertNewLine = true;
                    }
                    indent = lastPair.ValueIndent - 1;
                }
                else if (caretLine == lastPair.NameInterval.End.Line)
                {
                    insertNewLine = true;
                    var pair = (Pair) lastPair;
                    indent = lastPair.ValueIndent - 1;
                    if (pair.Delimiter == DelimiterEnum.C || pair.Delimiter == DelimiterEnum.CC) indent++;
                }
                else { indent = lastPair.ValueIndent; }
            }
            var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            string text = clipboard.WaitForText().TrimStart();
            string s4x = null;
            var converter = new XmlToSyntactikConverter(text);
            if (converter.Convert(indent, indentChar, indentMultiplicity, insertNewLine, out s4x))
            {
                textEditor.InsertAtCaret(s4x);
            }

        }

        private void GetIndentInfo(CompletionContext context, TextEditor textEditor, out char indentSymbol, out int indentMultiplicity)
        {
            var m = context.Context.CompileUnit.Modules[0];
            indentSymbol = m.IndentSymbol;
            indentMultiplicity = m.IndentMultiplicity;
            if (indentMultiplicity == 0)
            {
                var text = textEditor.GetLineText(textEditor.CaretLine);
                if (string.IsNullOrEmpty(text))
                {
                    indentMultiplicity = 1;
                    return;
                }
                indentMultiplicity = text.Length - text.TrimStart().Length;
                indentSymbol = text[0];
            }
        }

        //private XmlDocument GetXmlDocument(string text)
        //{
        //    var doc = new XmlDocument();
        //    try
        //    {
        //        XmlReaderSettings settings = new XmlReaderSettings
        //        {
        //            ConformanceLevel = ConformanceLevel.Fragment,
        //            ValidationFlags = XmlSchemaValidationFlags.None,
        //            ValidationType = ValidationType.None
        //        };
        //        using (var ms = new MemoryStream())
        //        {
        //            StreamWriter writer = new StreamWriter(ms);
        //            text = text.TrimStart(' ', '\r', '\n', '\t');
                    
        //            writer.Flush();
        //            ms.Position = 0;
        //            using (var xmlReader = new XmlTextReader(ms))
        //            {
        //                xmlReader.Namespaces = false;
        //                doc.Load(xmlReader);
        //            }
        //        }
        //    }
        //    catch (XmlException)
        //    {
        //        return null;
        //    }
        //    return doc;
        //}

        protected override void Update(CommandInfo info)
        {
            info.Enabled = false;
            var doc = IdeApp.Workbench.ActiveDocument;
            info.Visible = doc.FileName.Extension.ToLower() == ".s4x";

            if (!info.Visible) return;

            var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            if (!clipboard.WaitIsTextAvailable()) return ;
            info.Enabled = true;
        }


    }
}
