using System.Collections.Specialized;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content">View Content of the first tab (XML)</param>
        /// <param name="fileName"></param>
        /// <param name="mimeType"></param>
        /// <param name="ownerProject"></param>
        public SyntactikView(ViewContent content, FilePath fileName, string mimeType, Project ownerProject) : base(content)
        {
            var hiddenWindow = new HiddenWorkbenchWindow();
            _syntactikEditor = TextEditorFactory.CreateNewEditor();
            _syntactikEditor.MimeType = mimeType;
            _syntactikEditor.FileName = fileName;
            _viewContent = _syntactikEditor.GetContent<ViewContent>();
            _viewContent.ContentName = _syntactikEditor.FileName;
            hiddenWindow.AttachViewContent(_viewContent);
            _syntactikDocument = new SyntactikDocument(hiddenWindow, _syntactikEditor);
            _syntactikDocument.AttachToProject(ownerProject);
        }

        public override string TabPageLabel => _tabPageLabel;
        public ViewContent ViewContent => _viewContent;

        public TextEditor SyntactikEditor => _syntactikEditor;

        public Document SyntactikDocument => _syntactikDocument;

        protected override void OnPageShown(int npage)
        {
            base.OnPageShown(npage);
            switch (npage)
            {
                case 0:
                    if (_prevPage != npage)
                    {
                        SetXmlEditorText();
                        _prevPage = npage;
                        _tabPageLabel = "Xml";
                    }
                    break;
                case 1:
                    if (_prevPage != npage)
                    {
                        SetSyntactikEditorText();
                        _prevPage = npage;
                        _tabPageLabel = "Syntactik";
                    }
                    break;
            }
        }

        private void SetXmlEditorText()
        {
            var crc = Control.GetNativeWidget<CommandRouterContainer>();
            var editor = (TextEditor) ((ViewContent)crc.GetDelegatedCommandTarget()).Control;
            
            var document = IdeApp.Workbench.ActiveDocument;
            if (document == null) return;

            var syntactikView = IdeApp.Workbench.ActiveDocument.Window.ViewContent as SyntactikView;
            var textEditor = syntactikView?.SyntactikEditor;
            document = syntactikView?.SyntactikDocument;

            if (textEditor == null) return;
            var project = document.Project as SyntactikProject;
            var module = document.ParsedDocument?.Ast as Module;
            if (module == null || project == null) return;
            var modules = new PairCollection<Module> { module };

            var doc = module.ModuleDocument;
            if (doc == null) return;

            var compilerParameters = CreateCompilerParameters(project.CompilerContext, doc);
            var compiler = new SyntactikCompiler(compilerParameters);
            var context = compiler.Run(new CompileUnit { Modules = modules });

            object s;
            if (context.InMemoryOutputObjects.TryGetValue("CLIPBOARD", out s))
            {
                editor.Text = (string) s;
            }
        }

        protected virtual CompilerParameters CreateCompilerParameters(CompilerContext projectCompilerContext, DOM.Document doc)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new GenerateXmlForDocumentStep(projectCompilerContext, doc));
            return compilerParameters;
        }

        private void SetSyntactikEditorText()
        {
            var crc = Control.GetNativeWidget<CommandRouterContainer>();
            var editor = ((ViewContent) crc.GetDelegatedCommandTarget()).Control as TextEditor;
            var converter = new XmlToSyntactikConverter(editor?.Text, true, true);
            string s4x;
            if (converter.Convert(0, '\t', 1, false, new ListDictionary(), out s4x))
            {
                _syntactikEditor.Text = s4x;
            }
            else
            {
                DialogHelper.ShowError(
                    "The editor contains the invalid XML fragment that can't be converted to Syntactik format.",
                    null);
                ShowPage(0);
            }
        }

        public override Task Load(FileOpenInformation fileOpenInformation)
        {
            return base.Load(fileOpenInformation).ContinueWith(
                task =>
                {
                    return Runtime.RunInMainThread(delegate
                    {
                        AddButton("Syntactik", _syntactikEditor);

                        _syntactikEditor.TextChanged += (o, a) => {
                            if (_syntactikDocument.ParsedDocument != null)
                                _syntactikDocument.ParsedDocument.IsInvalid = true;
                                _syntactikDocument.ReparseDocument();
                        };
                    });
                });
        }
    }
}
