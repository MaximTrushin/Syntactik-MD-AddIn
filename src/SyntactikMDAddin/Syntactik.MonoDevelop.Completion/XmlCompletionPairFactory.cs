using System.Threading;
using Syntactik.Compiler;
using Syntactik.Compiler.Steps;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.IO;
using Module = Syntactik.DOM.Mapped.Module;


namespace Syntactik.MonoDevelop.Completion
{
    internal class XmlCompletionPairFactory : IPairFactory
    {
        private CancellationToken _cancellationToken;
        private readonly CompilerContext _context;
        private Module _module;
        private readonly IPairFactory _pairFactory;
        private Pair _lastPair;


        public XmlCompletionPairFactory(CompilerContext context, Syntactik.DOM.Module module,
            CancellationToken cancellationToken)
        {
            _context = context;
            _module = (Module) module;
            _cancellationToken = cancellationToken;
            _pairFactory = new ReportingPairFactoryForXml(context, module);
        }

        public Pair CreateMappedPair(ICharStream input, int nameQuotesType, Interval nameInterval,
            DelimiterEnum delimiter,
            Interval delimiterInterval, int valueQuotesType, Interval valueInterval, int valueIndent)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            IMappedPair pair;
            var nameText = GetNameText(input, nameQuotesType, nameInterval);
            if (nameQuotesType > 0)
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;

                pair = new DOM.Element(input)
                {
                    NameQuotesType = nameQuotesType,
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    ValueIndent = valueIndent
                };
            }
            else if (nameText.StartsWith("@"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.E;
                pair = new DOM.Attribute(input)
                {
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    ValueIndent = valueIndent
                };
            }
            else if (nameText.StartsWith("!$"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;
                pair = new DOM.AliasDefinition(input)
                {
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    ValueIndent = valueIndent
                };
            }
            else if (nameText.StartsWith("!#"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.EE;
                pair = new DOM.NamespaceDefinition(input)
                {
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    ValueIndent = valueIndent
                };
            }
            else if (nameText.StartsWith("!%"))
            {
                pair = new DOM.Parameter(input)
                {
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    ValueIndent = valueIndent
                };
            }
            else if (nameText.StartsWith("!"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;
                pair = new DOM.Document(input)
                {
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    ValueIndent = valueIndent
                };
            }
            else if (nameText.StartsWith("$"))
            {
                pair = new DOM.Alias(input)
                {
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    ValueIndent = valueIndent
                };
            }
            else if (nameText.StartsWith("%"))
            {
                pair = new DOM.Argument(input)
                {
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    ValueIndent = valueIndent
                };
            }
            else if (nameText.StartsWith("#"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;
                pair = new DOM.Scope(input)
                {
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval
                };
            }
            else
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;

                pair = new DOM.Element(input)
                {
                    NameQuotesType = nameQuotesType,
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    ValueIndent = valueIndent
                };
            }
            SetValueType(pair, delimiter, /*value.Item1*/ null, valueQuotesType);
            return (Pair) pair;
        }

        private void SetValueType(IMappedPair pair, DelimiterEnum delimiter, string value, int valueQuotesType)
        {
            switch (delimiter)
            {
                case DelimiterEnum.CE:
                    pair.ValueType = ValueType.PairValue;
                    return;
                case DelimiterEnum.EC:
                    pair.ValueType = ValueType.Concatenation;
                    return;
                case DelimiterEnum.ECC:
                    pair.ValueType = ValueType.LiteralChoice;
                    return;
                case DelimiterEnum.C:
                case DelimiterEnum.CC:
                    pair.ValueType = ValueType.Object;
                    return;
            }
            //if (value == null) return;

            //if (valueQuotesType == 1)
            //{
            //    pair.ValueType = GetJsonValueType(value, ValueType.SingleQuotedString);
            //    return;
            //}
            //if (valueQuotesType == 2)
            //{
            //    pair.ValueType = ValueType.DoubleQuotedString;
            //    return;
            //}
            //if (delimiter == DelimiterEnum.E)
            //{
            //    pair.ValueType = GetJsonValueType(value, ValueType.FreeOpenString);
            //    return;
            //}

            //if (delimiter == DelimiterEnum.EE)
            //{
            //    pair.ValueType = GetJsonValueType(value, ValueType.OpenString);
            //    return;
            //}
        }

        internal static string GetNameText(ICharStream input, int nameQuotesType, Interval nameInterval)
        {
            if (nameQuotesType == 0)
                return ((ITextSource) input).GetText(nameInterval.Begin.Index, nameInterval.End.Index);
            var c = ((ITextSource) input).GetChar(nameInterval.End.Index);
            if (nameQuotesType == 1)
                return c == '\''
                    ? ((ITextSource) input).GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index - 1)
                    : ((ITextSource) input).GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index);

            return c == '"'
                ? ((ITextSource) input).GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index - 1)
                : ((ITextSource) input).GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index);
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

        public void ProcessComment(int commentType, Interval commentInterval)
        {
            _cancellationToken.ThrowIfCancellationRequested();
        }
    }
}