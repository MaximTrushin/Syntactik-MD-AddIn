using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Parser
{
    internal class FoldingReportingPairFactory: IPairFactory
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


        public FoldingReportingPairFactory(IPairFactory pairFactory, SyntactikParsedDocument document)
        {
            _pairFactory = pairFactory;
            _document = document;
            _foldingStack = new Stack<FoldingInfo>();
        }
        public Pair CreateMappedPair(ICharStream input, int nameQuotesType, Interval nameInterval, DelimiterEnum delimiter, Interval delimiterInterval,
            int valueQuotesType, Interval valueInterval, int valueIndent)
        {
            var result = _pairFactory.CreateMappedPair(input, nameQuotesType, nameInterval, delimiter, delimiterInterval, valueQuotesType, valueInterval,
                valueIndent);

            if (delimiter != DelimiterEnum.E && delimiter != DelimiterEnum.EE)
            {
                _foldingStack.Push(new FoldingInfo {Pair = result, Begin = GetPairEnd(nameInterval, delimiterInterval), End = GetPairEnd(nameInterval, delimiterInterval) });    
            }

            return result;
        }

        public void AppendChild(Pair parent, Pair child)
        {
            if (child.Delimiter == DelimiterEnum.E || child.Delimiter == DelimiterEnum.EE)
            {
                if (_foldingStack.Count > 0)
                {
                    _foldingStack.Peek().End = GetPairEnd((IMappedPair) child);
                }
            }
            _pairFactory.AppendChild(parent, child);
        }

        public void EndPair(Pair pair, Interval endInterval)
        {
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

        private CharLocation GetPairEnd(Interval nameInterval, Interval delimiterInterval)
        {
            if (delimiterInterval != null) return delimiterInterval.End;
            return nameInterval.End;
        }

        private CharLocation GetPairEnd(IMappedPair child)
        {
            if (child.ValueInterval != null) return child.ValueInterval.End;
            if (child.DelimiterInterval != null) return child.DelimiterInterval.End;
            return child.NameInterval.End;
        }

    }
}
