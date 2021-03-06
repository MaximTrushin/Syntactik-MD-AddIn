﻿using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Content;
using Syntactik.Compiler;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.MonoDevelop.Completion;
using Syntactik.MonoDevelop.Converter;
using Syntactik.MonoDevelop.DisplayBinding;
using Syntactik.MonoDevelop.Licensing;
using Syntactik.MonoDevelop.Projects;
using Module = Syntactik.DOM.Module;
using NamespaceDefinition = Syntactik.DOM.NamespaceDefinition;

namespace Syntactik.MonoDevelop.Commands
{
    internal class PasteXmlHandler : CommandHandler //TODO: Unit tests
    {
        protected override void Run()
        {
            var document = IdeApp.Workbench.ActiveDocument;
            if (document == null) return;

            TextEditor textEditor;
            if (document.FileName.Extension.ToLower() == ".xml")
            {
                var syntactikView = IdeApp.Workbench.ActiveDocument.Window.ViewContent as SyntactikView;
                textEditor = syntactikView?.SyntactikEditor;
                document = syntactikView?.SyntactikDocument;
            }
            else
            {
                textEditor = IdeApp.Workbench.ActiveDocument?.Editor;
            }
            if (textEditor == null) return;
            var project = document.Project as SyntactikProject;
            var module = document.ParsedDocument?.Ast as Module;
            if (module == null || project == null) return;

            bool insertNewLine = false;
            var indent = 0;
            var indentChar = '\t';
            var indentMultiplicity = 1;
            ListDictionary declaredNamespaces = null;

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
                declaredNamespaces = GetDeclaredNamespaces(context.Context);
                GetIndentInfo(module, textEditor, out indentChar, out indentMultiplicity);
                var lastPair = context.LastPair as IMappedPair;
                if (lastPair != null)
                {

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
                        if (pair.Assignment == AssignmentEnum.C || pair.Assignment == AssignmentEnum.CC) indent++;
                    }
                    else
                    {
                        indent = lastPair.ValueIndent / indentMultiplicity;
                    }
                }
            }
            var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            var text = clipboard.WaitForText().TrimStart();
            using (var monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor())
            {
                try
                {
                    string s4x;
                    var converter = new XmlToSyntactikConverter(text);
                    var namespaces = new ListDictionary();
                    if (declaredNamespaces != null)
                        foreach (var declaredNamespace in declaredNamespaces)
                        {
                            var entry = (DictionaryEntry) declaredNamespace;
                            namespaces.Add(entry.Key, entry.Value);
                        }
                    if (converter.Convert(indent, indentChar, indentMultiplicity, insertNewLine, namespaces, out s4x))
                    {
                        using (textEditor.OpenUndoGroup())
                        {
                            textEditor.EnsureCaretIsNotVirtual();
                            AddMissingNamespaces(declaredNamespaces, namespaces, ext);
                            textEditor.InsertAtCaret(s4x);
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

        protected virtual string SuccessMessage => "Syntactik code is generated from XML in clipboard.";
        protected virtual string ErrorMessage => "Text in clipboard is not valid XML.";
        private void AddMissingNamespaces(ListDictionary declaredNamespaces, ListDictionary namespaces,
            SyntactikCompletionTextEditorExtension ext)
        {
            foreach (var item in namespaces)
            {
                var entry = (DictionaryEntry) item;
                if (!declaredNamespaces.Values.OfType<string>().Contains(entry.Value))
                {
                    ext.AddNewNamespaceToModule(entry.Key.ToString(), entry.Value.ToString());
                }
            }
        }

        internal static ListDictionary GetDeclaredNamespaces(CompilerContext context)
        {
            var result = new ListDictionary();
            var module = context.CompileUnit.Modules[0];
            foreach (var nsDef in module.NamespaceDefinitions)
            {
                AddNsDefToListDict(result, nsDef);
            }
            if (module.Members.Count <= 0) return result;

            var member = module.Members[0];
            foreach (var nsDef in member.NamespaceDefinitions)
            {
                AddNsDefToListDict(result, nsDef);
            }

            return result;
        }

        private static void AddNsDefToListDict(ListDictionary result, NamespaceDefinition nsDef)
        {
            foreach (var item in result)
            {
                var entry = (DictionaryEntry) item;
                if (entry.Value.ToString() == nsDef.Value)
                {
                    entry.Key = nsDef.Name;
                    return;
                }
                if (entry.Key.ToString() == nsDef.Name)
                {
                    entry.Value = nsDef.Value;
                    return;
                }
            }
            result.Add(nsDef.Name, nsDef.Value);
        }

        internal static void GetIndentInfo(Module module, TextEditor textEditor, out char indentSymbol,
            out int indentMultiplicity)
        {
            indentSymbol = module.IndentSymbol;
            indentMultiplicity = module.IndentMultiplicity;
            if (indentMultiplicity != 0) return;

            var text = textEditor.GetLineText(textEditor.CaretLine);
            if (string.IsNullOrEmpty(text))
            {
                if (indentSymbol == 0) indentSymbol = '\t'; //todo: read this from code formatting settings (replace tabs with spaces)
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
            string extension;
            if (doc.FileName.Extension.ToLower() == ".xml" && doc.Window.ViewContent?.TabPageLabel == "Syntactik")
            {
                extension = ".s4x";
            }
            else
            {
                extension = doc.FileName.Extension.ToLower();
            }
            info.Visible = extension == ".s4x";

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