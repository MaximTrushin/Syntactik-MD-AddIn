using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using Syntactik.Compiler;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.MonoDevelop.Projects;
using Document = Syntactik.DOM.Document;
using Module = Syntactik.DOM.Module;

namespace Syntactik.MonoDevelop.Commands
{
    public class CopyDocAsXmlHandler : CommandHandler //TODO: Unit tests
    {

        protected override void Run()
        {
            var textEditor = IdeApp.Workbench.ActiveDocument?.Editor;
            if (textEditor == null) return;
            var document = IdeApp.Workbench.ActiveDocument;
            var project = document.Project as SyntactikProject;
            var module = document?.ParsedDocument?.Ast as Module;
            if (module == null || project == null) return;
            var modules = new PairCollection<Module> { module };

            Document doc = FindCurrentDocument(module, textEditor.CaretOffset);
            if (doc == null) return;

            var compilerParameters = CreateCompilerParameters(project.CompilerContext, doc);
            var compiler = new SyntactikCompiler(compilerParameters);
            var context = compiler.Run(new CompileUnit { Modules = modules });
            var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            clipboard.Text = (string)context.InMemoryOutputObjects["CLIPBOARD"];
        }

        private CompilerParameters CreateCompilerParameters(CompilerContext projectCompilerContext, Document doc)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new GenerateXmlForDocumentStep(projectCompilerContext, doc));
            return compilerParameters;
        }

        private static Document FindCurrentDocument(Module module, int textEditorCaretOffset)
        {
            var list = module.Members.OfType<Pair>();
            if (module.ModuleDocument != null)
                list = list.Concat(module.ModuleDocument?.Entities).OrderBy(
                    p =>
                    {
                        var mapped = (IMappedPair) p;
                        if (mapped.NameInterval != null) return mapped.NameInterval.Begin.Index;
                        return mapped.DelimiterInterval.Begin.Index;
                    }
                );
            Pair offsetPair = null;
            foreach (var pair in list.Reverse())
            {
                var mappedPair = (IMappedPair)pair;
                if (mappedPair.NameInterval.Begin.Index > textEditorCaretOffset) continue;

                offsetPair = pair;
                break;
            }
            return offsetPair != null ? FindParentDocument(offsetPair) : module.ModuleDocument;
        }

        private static Document FindParentDocument(Pair pair)
        {
            while (pair != null)
            {
                var document = pair as Document;
                if (document != null) return document;
                pair = pair.Parent;
            }

            return null;
        }

        protected override void Update(CommandInfo info)
        {
            info.Enabled = false;
            var doc = IdeApp.Workbench.ActiveDocument;
            info.Visible = doc.FileName.Extension.ToLower() == ".s4x";

            if (!info.Visible) return;
            info.Enabled = true;
        }
    }
}
