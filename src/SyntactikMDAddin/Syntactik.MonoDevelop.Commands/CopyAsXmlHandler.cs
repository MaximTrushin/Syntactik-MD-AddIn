using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using Syntactik.Compiler;
using Syntactik.DOM;
using Syntactik.MonoDevelop.DisplayBinding;
using Syntactik.MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.Commands
{
    public class CopyAsXmlHandler: CommandHandler  //TODO: Unit tests
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

            var modules = new PairCollection<Module> {module};
            var compilerParameters = CreateCompilerParameters(project.CompilerContext, textEditor.SelectionRange);
            var compiler = new SyntactikCompiler(compilerParameters);
            var context = compiler.Run(new CompileUnit {Modules = modules });
            using (var monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor())
            {
                object s;
                if (context.InMemoryOutputObjects.TryGetValue("CLIPBOARD", out s))
                {
                    var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
                    clipboard.Text = (string) context.InMemoryOutputObjects["CLIPBOARD"];
                    monitor.ReportSuccess(SuccessMessage);
                }
                else
                {
                    monitor.ReportError(ErrorMessage);
                }
            }
        }

        protected virtual string SuccessMessage => "XML is copied to clipboard.";
        protected virtual string ErrorMessage => "Selection can't be converted to valid XML.";

        protected virtual CompilerParameters CreateCompilerParameters(CompilerContext projectCompilerContext, ISegment textEditorSelectionRange)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new GenerateXmlForSelectionStep(projectCompilerContext, textEditorSelectionRange));
            return compilerParameters;
        }

        protected override void Update(CommandInfo info)
        {
            info.Enabled = false;
            info.Visible = false;

            var doc = IdeApp.Workbench.ActiveDocument;
            if (doc == null)
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

            TextEditor textEditor;
            if (doc.FileName.Extension.ToLower() == ".xml")
            {
                var syntactikView = IdeApp.Workbench.ActiveDocument.Window.ViewContent as SyntactikView;
                textEditor = syntactikView?.SyntactikEditor;
            }
            else
            {
                textEditor = IdeApp.Workbench.ActiveDocument?.Editor;
            }

            if (!string.IsNullOrEmpty(textEditor?.SelectedText))
                info.Enabled = true;
        }
    }
}