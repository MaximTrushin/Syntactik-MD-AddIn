using System.Threading;
using Syntactik.Compiler;
using Syntactik.Compiler.Steps;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.IO;
using Element = Syntactik.DOM.Mapped.Element;
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


        public XmlCompletionPairFactory(CompilerContext context, Syntactik.DOM.Module module, CancellationToken cancellationToken)
        {
            _context = context;
            _module = (Module) module;
            _cancellationToken = cancellationToken;
            _pairFactory = new ReportingPairFactoryForXml(context, module);
        }

        public Pair CreateMappedPair(ICharStream input, int nameQuotesType, Interval nameInterval, DelimiterEnum delimiter,
            Interval delimiterInterval, int valueQuotesType, Interval valueInterval, int valueIndent)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            IMappedPair pair;
            var nameText = GetNameText(input, nameQuotesType, nameInterval);
            //var value = GetValue(input, delimiter, valueQuotesType, valueInterval, valueIndent, _context, _module);
            if (nameQuotesType > 0)
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;

                pair = new DOM.Element(input)
                {
                    //Name = VerifyElementName(nameText, nameInterval, _module),
                    NameQuotesType = nameQuotesType,
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    //Value = value.Item1,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    //InterpolationItems = value.Item2,
                    ValueIndent = valueIndent
                };

            }
            else if (nameText.StartsWith("@"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.E;

                var tuple = Element.GetNameAndNs(nameText.Substring(1), nameQuotesType);
                var ns = string.IsNullOrEmpty(tuple.Item1) ? null : tuple.Item1;
                pair = new DOM.Attribute
                {
                    NsPrefix = ns,
                    //Name = VerifyName(tuple.Item2, nameInterval, _module),
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    //Value = value.Item1,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    //InterpolationItems = value.Item2,
                    ValueIndent = valueIndent
                };

            }
            else if (nameText.StartsWith("!$"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;
                pair = new DOM.AliasDefinition(input)
                {
                    //Name = VerifyName(nameText.Substring(2), nameInterval, _module),
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    //Value = value.Item1,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    //InterpolationItems = value.Item2,
                    ValueIndent = valueIndent
                };
            }
            else if (nameText.StartsWith("!#"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.EE;
                pair = new DOM.NamespaceDefinition
                {
                    //Name = VerifyNsName(nameText.Substring(2), nameInterval, _module),
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    //Value = value.Item1,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    //InterpolationItems = value.Item2,
                    ValueIndent = valueIndent
                };
            }
            else if (nameText.StartsWith("!%"))
            {
                pair = new DOM.Parameter
                {
                    //Name = VerifyNsName(nameText.Substring(2), nameInterval, _module),
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    //Value = value.Item1,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    //InterpolationItems = value.Item2,
                    ValueIndent = valueIndent
                };
            }
            else if (nameText.StartsWith("!"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;
                pair = new DOM.Document
                {
                    //Name = VerifyName(nameText.Substring(1), nameInterval, _module),
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    //Value = value.Item1,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    //InterpolationItems = value.Item2,
                    ValueIndent = valueIndent
                };
            }
            else if (nameText.StartsWith("$"))
            {
                pair = new DOM.Alias(input)
                {
                    //Name = VerifyName(nameText.Substring(1), nameInterval, _module),
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    //Value = value.Item1,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    //InterpolationItems = value.Item2,
                    ValueIndent = valueIndent
                };

            }
            else if (nameText.StartsWith("%"))
            {
                pair = new DOM.Argument(input)
                {
                    //Name = VerifyName(nameText.Substring(1), nameInterval, _module),
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    //Value = value.Item1,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    //InterpolationItems = value.Item2,
                    ValueIndent = valueIndent
                };
            }
            else if (nameText.StartsWith("#"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;
                pair = new DOM.Scope
                {
                    //NsPrefix = VerifyScopeName(nameText.Substring(1), nameInterval, _module),
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval
                };
            }
            else
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;
                //var tuple = Element.GetNameAndNs(nameText, nameQuotesType);
                //var ns = string.IsNullOrEmpty(tuple.Item1) ? null : tuple.Item1;

                pair = new DOM.Element(input)
                {
                    //NsPrefix = ns,
                    //Name = VerifyElementName(tuple.Item2, nameInterval, _module),
                    NameQuotesType = nameQuotesType,
                    NameInterval = nameInterval,
                    Delimiter = delimiter,
                    DelimiterInterval = delimiterInterval,
                    //Value = value.Item1,
                    ValueQuotesType = valueQuotesType,
                    ValueInterval = valueInterval,
                    //InterpolationItems = value.Item2,
                    ValueIndent = valueIndent
                };

            }
            SetValueType(pair, delimiter, /*value.Item1*/ null, valueQuotesType);
            return (Pair)pair;
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
                return ((ITextSource)input).GetText(nameInterval.Begin.Index, nameInterval.End.Index);
            var c = ((ITextSource)input).GetChar(nameInterval.End.Index);
            if (nameQuotesType == 1)
                return c == '\'' ? ((ITextSource)input).GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index - 1) : ((ITextSource)input).GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index);

            return c == '"' ? ((ITextSource)input).GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index - 1) : ((ITextSource)input).GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index);

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
