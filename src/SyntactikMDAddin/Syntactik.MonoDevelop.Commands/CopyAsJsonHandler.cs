using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using Syntactik.Compiler;

namespace Syntactik.MonoDevelop.Commands
{

    class CopyAsJsonHandler : CopyAsXmlHandler  //TODO: Unit tests 
    {

        protected override CompilerParameters CreateCompilerParameters(CompilerContext projectCompilerContext, ISegment textEditorSelectionRange)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new GenerateJsonForSelectionStep(projectCompilerContext, textEditorSelectionRange));
            return compilerParameters;
        }

        protected override string SuccessMessage => "JSON is copied to clipboard.";
        protected override string ErrorMessage => "Selection can't be converted to valid JSON.";

        protected override void Update(CommandInfo info)
        {
            info.Enabled = false;
            var doc = IdeApp.Workbench.ActiveDocument;
            if (doc == null)
            {
                info.Bypass = true;
                return;
            }
            info.Visible = doc.FileName.Extension.ToLower() == ".s4j";

            if (!info.Visible) return;
            
            var textEditor = IdeApp.Workbench.ActiveDocument?.Editor;
            if (!string.IsNullOrEmpty(textEditor?.SelectedText))
                info.Enabled = true;
        }
    }
}
