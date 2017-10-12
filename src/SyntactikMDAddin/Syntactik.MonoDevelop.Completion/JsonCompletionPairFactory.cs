﻿using System.Threading;
using Syntactik.Compiler;
using Syntactik.Compiler.Steps;
using Syntactik.DOM;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Completion
{
    internal class JsonCompletionPairFactory : IPairFactory
    {
        private CancellationToken _cancellationToken;
        private readonly CompilerContext _context;
        private readonly IPairFactory _pairFactory;
        private Pair _lastPair;


        public JsonCompletionPairFactory(CompilerContext context, Module module, CancellationToken cancellationToken)
        {
            _context = context;
            _cancellationToken = cancellationToken;
            _pairFactory = new ReportingPairFactoryForJson(context, module);
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

        public void EndPair(Pair pair, Interval endInterval, bool endedByEof = false)
        {
            if (endedByEof && _lastPair == null)
            {
                _lastPair = pair;
                _context.InMemoryOutputObjects.Add("LastPair", pair);
            }
            _cancellationToken.ThrowIfCancellationRequested();
        }

        public Comment ProcessComment(ICharStream input, int commentType, Interval commentInterval)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return null;
        }
    }
}
