using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;

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

        public override Task<ICompletionDataList> HandleCodeCompletionAsync(CodeCompletionContext completionContext, char completionChar, CancellationToken token = default(CancellationToken))
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
            var currentLocation = new DocumentLocation(completionContext.TriggerLine, completionContext.TriggerLineOffset);
            char currentChar = completionContext.TriggerOffset < 1 ? ' ' : buf.GetCharAt(completionContext.TriggerOffset - 1);
            char previousChar = completionContext.TriggerOffset < 2 ? ' ' : buf.GetCharAt(completionContext.TriggerOffset - 2);

            if (currentChar == '$')
            {
                var list = new CompletionDataList();

                list.Add("Parsing file").Description = "parsing ...";
                return list;
            }
            else
            {
                
            }

            return null;
        }

        //public override Task<ICompletionDataList> CodeCompletionCommand(CodeCompletionContext completionContext)
            //{
            //    //return InternalHandleCodeCompletion(completionContext, ch, true, triggerWordLength, default(CancellationToken));
            //    return Task.FromResult(GetCompletionList(completionContext));
            //}
        }
}