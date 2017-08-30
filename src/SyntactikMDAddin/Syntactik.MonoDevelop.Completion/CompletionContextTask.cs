using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core.Text;

namespace Syntactik.MonoDevelop.Completion
{
    class CompletionContextTask
    {

        public CompletionContextTask(Task<CompletionContext> task, ITextSourceVersion version, int offset, CancellationToken token) 
        {
            CancellationToken = token;
            Task = task;
            Version = version;
            Offset = offset;
        }

        public CancellationToken CancellationToken { get; }

        public ITextSourceVersion Version { get; }
        public int Offset { get; }

        public Task<CompletionContext> Task { get; }
    }
}
