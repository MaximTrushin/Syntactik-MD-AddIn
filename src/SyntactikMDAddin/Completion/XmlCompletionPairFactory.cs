using System.Threading;
using Syntactik.Compiler;
using Syntactik.Compiler.Steps;
using Syntactik.DOM;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Completion
{
    internal class XmlCompletionPairFactory : IPairFactory
    {
        private CancellationToken _cancellationToken;
        private CompilerContext _context;
        private Module _module;
        private readonly IPairFactory _pairFactory;


        public XmlCompletionPairFactory(CompilerContext context, Module module, CancellationToken cancellationToken)
        {
            _context = context;
            _module = module;
            _cancellationToken = cancellationToken;
            _pairFactory = new ReportingPairFactoryForXml(context, module);
        }

        public Pair CreateMappedPair(ICharStream input, int nameQuotesType, Interval nameInterval, DelimiterEnum delimiter,
            Interval delimiterInterval, int valueQuotesType, Interval valueInterval, int valueIndent)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return _pairFactory.CreateMappedPair(input, nameQuotesType, nameInterval, delimiter, delimiterInterval, 
                        valueQuotesType, valueInterval, valueIndent);
        }

        public void AppendChild(Pair parent, Pair child)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _pairFactory.AppendChild(parent, child);
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
