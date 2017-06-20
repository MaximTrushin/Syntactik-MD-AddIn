using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Syntactik.Compiler;
using Syntactik.DOM;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Completion
{
    internal class JsonCompletionPairFactory : IPairFactory
    {
        private CancellationToken _cancellationToken;
        private CompilerContext _context;
        private Module _module;


        public JsonCompletionPairFactory(CompilerContext _context, Module module, CancellationToken _cancellationToken)
        {
            this._context = _context;
            this._module = module;
            this._cancellationToken = _cancellationToken;
        }

        public Pair CreateMappedPair(ICharStream input, int nameQuotesType, Interval nameInterval, DelimiterEnum delimiter,
            Interval delimiterInterval, int valueQuotesType, Interval valueInterval, int valueIndent)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        public void AppendChild(Pair parent, Pair child)
        {
            _cancellationToken.ThrowIfCancellationRequested();
        }

        public void EndPair(Pair pair, Interval endInterval)
        {
            _cancellationToken.ThrowIfCancellationRequested();
        }

        public void ProcessComment(int commentType, Interval commentInterval)
        {
            _cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
