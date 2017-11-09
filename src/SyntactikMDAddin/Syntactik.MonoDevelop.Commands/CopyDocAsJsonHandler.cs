using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using Syntactik.Compiler;
using Syntactik.MonoDevelop.Licensing;
using Syntactik.MonoDevelop.Projects;
using Document = Syntactik.DOM.Document;

namespace Syntactik.MonoDevelop.Commands
{
    class CopyDocAsJsonHandler : CopyDocAsXmlHandler //TODO: Unit tests
    {
        protected override string SuccessMessage => "JSON document is copied to clipboard.";
        protected override string ErrorMessage => "Document can't be converted to valid JSON.";
        protected override CompilerParameters CreateCompilerParameters(CompilerContext projectCompilerContext, Document doc)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new GenerateJsonForDocumentStep(projectCompilerContext, doc));
            return compilerParameters;
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
            info.Enabled = true;
        }
    }
}
