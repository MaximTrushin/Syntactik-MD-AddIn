using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using Syntactik.Compiler;
using Syntactik.Compiler.IO;
using Syntactik.MonoDevelop.Completion.DOM;
using Syntactik.MonoDevelop.Parser;

namespace Syntactik.MonoDevelop.Completion
{
    public class SyntactikCompletionTextEditorExtension : CompletionTextEditorExtension
    {
        public override string CompletionLanguage => "S4X";

        protected override void Initialize()
        {
            base.Initialize();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override Task<ICompletionDataList> CodeCompletionCommand(CodeCompletionContext completionContext)
        {
            int pos = completionContext.TriggerOffset;
            if (pos <= 0)
                return null;
            return HandleCodeCompletion(completionContext, true, default(CancellationToken));
        }

        public override Task<ICompletionDataList> HandleCodeCompletionAsync(CodeCompletionContext completionContext,
            char completionChar, CancellationToken token = default(CancellationToken))
        {
            int pos = completionContext.TriggerOffset;
            char ch = Editor.GetCharAt(pos - 1);
            if (pos > 0 && ch == completionChar)
            {
                //tracker.UpdateEngine();
                return HandleCodeCompletion(completionContext, false, token);
            }
            return null;
        }

        protected virtual async Task<ICompletionDataList> HandleCodeCompletion(
            CodeCompletionContext completionContext, bool forced, CancellationToken token)
        {
            var buf = this.Editor;
            var compilerParameters = CreateCompilerParameters(Editor.FileName, Editor.Text, Editor.CaretOffset);
            var compiler = new SyntactikCompiler(compilerParameters);
            var context = compiler.Run();

            var currentLocation = new DocumentLocation(completionContext.TriggerLine,
                completionContext.TriggerLineOffset);
            char currentChar = completionContext.TriggerOffset < 1
                ? ' '
                : buf.GetCharAt(completionContext.TriggerOffset - 1);
            char previousChar = completionContext.TriggerOffset < 2
                ? ' '
                : buf.GetCharAt(completionContext.TriggerOffset - 2);

            var visitor = new CompletionContextVisitor();
            visitor.Visit(context.CompileUnit);
            var p = visitor.LastPair;

            //if (currentChar == '$')
            //{
            //    var list = new CompletionDataList();

            //    list.Add("Parsing file").Description = "parsing ...";
            //    return list;
            //}


            return null;
        }

        private CompilerParameters CreateCompilerParameters(string fileName, string content, int offset,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var compilerParameters = new CompilerParameters {Pipeline = new CompilerPipeline()};
            compilerParameters.Pipeline.Steps.Add(new ParseForCompletionStep(cancellationToken));
            compilerParameters.Input.Add(new StringInput(fileName, content.Substring(0, offset + 1)));
            return compilerParameters;

            //public override Task<ICompletionDataList> CodeCompletionCommand(CodeCompletionContext completionContext)
            //{
            //    //return InternalHandleCodeCompletion(completionContext, ch, true, triggerWordLength, default(CancellationToken));
            //    return Task.FromResult(GetCompletionList(completionContext));
            //}
        }
    }
}