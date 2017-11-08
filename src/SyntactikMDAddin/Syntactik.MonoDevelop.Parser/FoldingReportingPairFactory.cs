using System.Collections;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.IO;
using Syntactik.MonoDevelop.Completion.DOM;

namespace Syntactik.MonoDevelop.Parser
{
     class FoldingReportingPairFactory: IPairFactory
    {
        internal class FoldingInfo
        {
            public Pair Pair;
            public CharLocation Begin;
            public CharLocation End;
        }

        private readonly Stack<FoldingInfo> _foldingStack;

        private readonly IPairFactory _pairFactory;
        private readonly SyntactikParsedDocument _document;
        private readonly CancellationToken _cancellationToken;


        public FoldingReportingPairFactory(IPairFactory pairFactory, SyntactikParsedDocument document, CancellationToken cancellationToken)
        {
            _pairFactory = pairFactory;
            _document = document;
            _cancellationToken = cancellationToken;
            _foldingStack = new Stack<FoldingInfo>();
        }
        public Pair CreateMappedPair(ICharStream input, int nameQuotesType, Interval nameInterval, DelimiterEnum delimiter, Interval delimiterInterval,
            int valueQuotesType, Interval valueInterval, int valueIndent)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var result = _pairFactory.CreateMappedPair(input, nameQuotesType, nameInterval, delimiter, delimiterInterval, valueQuotesType, valueInterval,
                valueIndent);

            if (delimiter != DelimiterEnum.E && delimiter != DelimiterEnum.EE && delimiter != DelimiterEnum.None)
            {
                _foldingStack.Push(new FoldingInfo {Pair = result, Begin = DomHelper.GetPairEnd(nameInterval, delimiterInterval), End = DomHelper.GetPairEnd(nameInterval, delimiterInterval) });    
            }

            return result;
        }

        public void AppendChild(Pair parent, Pair child)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (child.Delimiter == DelimiterEnum.E || child.Delimiter == DelimiterEnum.EE)
            {
                if (_foldingStack.Count > 0)
                {
                    _foldingStack.Peek().End = DomHelper.GetPairEnd((IMappedPair) child);
                }

                if (((IMappedPair)child).ValueInterval != null && ((IMappedPair) child).ValueInterval.Begin.Line != ((IMappedPair) child).ValueInterval.End.Line)
                {
                    _document.Foldings.Add(new FoldingRegion("...",
                        new DocumentRegion(((IMappedPair)child).ValueInterval.Begin.Line, 
                            ((IMappedPair)child).ValueInterval.Begin.Column, 
                            ((IMappedPair)child).ValueInterval.End.Line,
                            ((IMappedPair)child).ValueInterval.End.Column + 1),
                            FoldType.Undefined, false));
                }
            }
            _pairFactory.AppendChild(parent, child);
        }

        public void EndPair(Pair pair, Interval endInterval, bool endedByEof = false)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (_foldingStack.Count == 0) return;
            var foldingInfo = _foldingStack.Peek();
            if (pair == foldingInfo.Pair)
            {
                _foldingStack.Pop();
                if (foldingInfo.Begin.Line != foldingInfo.End.Line)
                {
                    _document.Foldings.Add(new FoldingRegion("...",
                        new DocumentRegion(foldingInfo.Begin.Line, foldingInfo.Begin.Column + 1, foldingInfo.End.Line,
                            foldingInfo.End.Column + 1),
                        FoldType.Undefined, false));
                }
                if (_foldingStack.Count > 0)
                {
                    var parentFoldingInfo = _foldingStack.Peek();
                    parentFoldingInfo.End = foldingInfo.End;
                }
            }
            else
            { 
                //pair == null, this is closing bracket
                foldingInfo.End = endInterval.End;

            }
            _pairFactory.EndPair(pair, endInterval);
        }



        public DOM.Comment ProcessComment(ICharStream input, int commentType, Interval commentInterval)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (commentInterval.Begin.Line != commentInterval.End.Line)
                _document.Foldings.Add(new FoldingRegion("...",
                    new DocumentRegion(commentInterval.Begin.Line, commentInterval.Begin.Column, commentInterval.End.Line,
                        commentInterval.End.Column + 1), FoldType.Comment, false));

            return _pairFactory.ProcessComment(input, commentType, commentInterval);
        }
    }
}
