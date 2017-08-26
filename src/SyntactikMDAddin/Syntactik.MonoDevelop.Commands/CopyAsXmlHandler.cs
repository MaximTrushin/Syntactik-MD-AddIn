using Mono.TextEditor;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using Syntactik.Compiler;
using Syntactik.DOM;
using Syntactik.MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.Commands
{
    public class CopyAsXmlHandler : CommandHandler
    {
        protected override void Run()
        {
            var textEditor = IdeApp.Workbench.ActiveDocument?.Editor;
            if (textEditor == null) return;
            var document = IdeApp.Workbench.ActiveDocument;
            var project = document.Project as SyntactikProject;
            var module = document?.ParsedDocument?.Ast as Module;
            if (module == null || project == null) return;

            var modules = new PairCollection<Module> {module};
            var compilerParameters = CreateCompilerParameters(project.CompilerContext, textEditor.SelectionRange);
            var compiler = new SyntactikCompiler(compilerParameters);
            var context = compiler.Run(new CompileUnit {Modules = modules });

            var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            clipboard.Text = (string) context.InMemoryOutputObjects["CLIPBOARD"];
        }
        private CompilerParameters CreateCompilerParameters(CompilerContext projectCompilerContext, ISegment textEditorSelectionRange)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new GenerateXmlForSelectionStep(projectCompilerContext, textEditorSelectionRange));
            return compilerParameters;
        }

        protected override void Update(CommandInfo info)
        {
            info.Enabled = false;
            var doc = IdeApp.Workbench.ActiveDocument;
            info.Visible = doc.FileName.Extension.ToLower() == ".s4x";

            if (!info.Visible) return;
            
            if (!IdeApp.Workbench.RootWindow.HasToplevelFocus) return;

            var textEditor = IdeApp.Workbench.RootWindow.Focus as TextArea;
            if (!string.IsNullOrEmpty(textEditor?.SelectedText))
                info.Enabled = true;
        }
    }
}
