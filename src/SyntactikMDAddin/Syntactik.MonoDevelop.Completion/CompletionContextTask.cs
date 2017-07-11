using System.Threading.Tasks;
using MonoDevelop.Core.Text;

namespace Syntactik.MonoDevelop.Completion
{
    class CompletionContextTask
    {
        public CompletionContextTask(Task<CompletionContext> task, ITextSourceVersion version) 
        {
            Task = task;
            Version = version;
        }

        public ITextSourceVersion Version { get; set; }

        public Task<CompletionContext> Task { get; }
    }
}
