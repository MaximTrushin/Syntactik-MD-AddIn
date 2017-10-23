using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Syntactik.Compiler;
using Syntactik.DOM;
using Syntactik.MonoDevelop.Commands;
using Syntactik.MonoDevelop.Converter;
using Syntactik.MonoDevelop.Projects;
using Syntactik.MonoDevelop.Util;
using Document = MonoDevelop.Ide.Gui.Document;

namespace Syntactik.MonoDevelop.DisplayBinding
{
    public class SyntactikView : CombinedDesignView
    {
        private readonly TextEditor _syntactikEditor;
        private readonly ViewContent _viewContent;
        private int _prevPage;
        private readonly Document _syntactikDocument;
        private string _tabPageLabel = "Xml";
        private bool _textChanged;
        private readonly ViewContent _xmlContent;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content">View Content of the first tab (XML)</param>
        /// <param name="fileName"></param>
        /// <param name="mimeType"></param>
        /// <param name="ownerProject"></param>
        public SyntactikView(ViewContent content, FilePath fileName, string mimeType, Project ownerProject) : base(content)
        {
            _xmlContent = content;
            var hiddenWindow = new HiddenWorkbenchWindow();
            _syntactikEditor = TextEditorFactory.CreateNewEditor();
            _syntactikEditor.MimeType = mimeType;
            _syntactikEditor.FileName = Path.ChangeExtension(fileName, "s4x"); 
            
            _viewContent = _syntactikEditor.GetContent<ViewContent>();
            _viewContent.ContentName = fileName; //Tooltip error window needs original file name
            hiddenWindow.AttachViewContent(_viewContent);
            _syntactikDocument = new SyntactikDocument(hiddenWindow, _syntactikEditor);
            _syntactikDocument.AttachToProject(ownerProject);

            AddButton("Syntactik", _syntactikEditor);
            _syntactikEditor.TextChanged += (o, a) => {
                if (_syntactikDocument.ParsedDocument != null)
                    _syntactikDocument.ParsedDocument.IsInvalid = true;
                _syntactikDocument.ReparseDocument();
            };
        }

        public override string TabPageLabel => _tabPageLabel;
        public ViewContent ViewContent => _viewContent;

        public TextEditor SyntactikEditor => _syntactikEditor;

        public Document SyntactikDocument => _syntactikDocument;

        public override Task Save(FileSaveInformation fileSaveInformation)
        {
            if (TabPageLabel == "Xml")
                return base.Save(fileSaveInformation);

            if (SetXmlEditorText())
            {
                return base.Save(fileSaveInformation);
            }

            const string msg = "The editor contains invalid Syntactik fragment that can't be converted to Xml." + 
                               "\nPress Ok to ignore your changes and save original Xml View." +
                               "\nPress Cancel to return to Syntactik view.";
            var md = new Gtk.MessageDialog(null, Gtk.DialogFlags.Modal, Gtk.MessageType.Question, Gtk.ButtonsType.OkCancel, msg);
            var result = DialogHelper.ShowCustomDialog(md);
            if (result == (int) Gtk.ResponseType.Ok)
            {
                return base.Save(fileSaveInformation);
            }
            ShowPage(1);
            return Task.FromResult(false);
        }

        protected override void OnPageShown(int npage)
        {
            switch (npage)
            {
                case 0:
                    if (_prevPage != npage)
                    {
                        _syntactikEditor.TextChanged -= SyntactikEditorOnTextChanged;
                        if (!SetXmlEditorText())
                        {
                            const string msg = "The editor contains invalid Syntactik fragment that can't be converted to Xml. \nPress Ok to return to Syntactik View." +
                                               "\nPress Cancel to ignore your changes and return to Xml View.";
                            var md = new Gtk.MessageDialog(null, Gtk.DialogFlags.Modal, Gtk.MessageType.Question, Gtk.ButtonsType.OkCancel, msg);
                            var result = DialogHelper.ShowCustomDialog(md);

                            if (result == (int) Gtk.ResponseType.Ok)
                            {
                                ShowPage(1);
                                return;
                            }
                        }
                        _prevPage = npage;
                        _tabPageLabel = "Xml";
                    }
                    break;
                case 1:
                    if (_prevPage != npage)
                    {
                        if (!SetSyntactikEditorText())
                        {
                            DialogHelper.ShowError(
                            "The editor contains invalid XML fragment that can't be converted to Syntactik format.",
                            null);
                            ShowPage(0);
                            return;
                        }
                        _prevPage = npage;
                        _tabPageLabel = "Syntactik";
                        _textChanged = false;
                        _syntactikEditor.TextChanged += SyntactikEditorOnTextChanged;
                    }
                    break;
            }
            base.OnPageShown(npage);
        }

        private void SyntactikEditorOnTextChanged(object sender, TextChangeEventArgs e)
        {
            _xmlContent.IsDirty = true;
            _textChanged = true;
        }

        /// <summary>
        /// Converts content of Syntactik editor and assigns it to xml editor.
        /// </summary>
        /// <returns>true if Syntactik editor was successfully synced with Xml editor.</returns>
        private bool SetXmlEditorText()
        {
            if (!_textChanged) return true;
            var crc = Control.GetNativeWidget<CommandRouterContainer>();
            var editor = (TextEditor) ((ViewContent)crc.GetDelegatedCommandTarget()).Control;
            
            var document = IdeApp.Workbench.ActiveDocument;
            if (document == null) return false;

            var syntactikView = IdeApp.Workbench.ActiveDocument.Window.ViewContent as SyntactikView;
            var textEditor = syntactikView?.SyntactikEditor;
            document = syntactikView?.SyntactikDocument;

            if (textEditor == null) return false;
            var project = document.Project as SyntactikProject;
            var module = document.ParsedDocument?.Ast as Module;
            if (module == null || project == null) return false;
            var modules = new PairCollection<Module> { module };

            var doc = module.ModuleDocument;
            if (doc == null) return false;

            if (document.ParsedDocument.HasErrors)
            {
                return false;
            }

            var compilerParameters = CreateCompilerParameters(project.CompilerContext, doc);
            var compiler = new SyntactikCompiler(compilerParameters);
            var context = compiler.Run(new CompileUnit { Modules = modules });

            object s;
            if (!context.InMemoryOutputObjects.TryGetValue("CLIPBOARD", out s)) return false;

            using (editor.OpenUndoGroup())
            {
                editor.EnsureCaretIsNotVirtual();
                editor.Text = "";
                editor.InsertText(0, (string)s);
                return true;
            }
        }

        protected virtual CompilerParameters CreateCompilerParameters(CompilerContext projectCompilerContext, DOM.Document doc)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new GenerateXmlForDocumentStep(projectCompilerContext, doc));
            return compilerParameters;
        }

        private bool SetSyntactikEditorText()
        {
            var crc = Control.GetNativeWidget<CommandRouterContainer>();
            var editor = ((ViewContent) crc.GetDelegatedCommandTarget()).Control as TextEditor;
            var converter = new XmlToSyntactikConverter(editor?.Text, true, true);
            string s4x;
            if (converter.Convert(0, '\t', 1, false, new ListDictionary(), out s4x))
            {
                _syntactikEditor.Text = s4x;
                return true;
            }
            return false;
        }

        public override void DiscardChanges()
        {
            base.DiscardChanges();
            _xmlContent.DiscardChanges();
            _viewContent.DiscardChanges();
        }
    }
}
