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
            _pairFactory = new PairFactoryForXml(context, module);
        }

        public Pair CreateMappedPair(ITextSource input, int nameQuotesType, Interval nameInterval,
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

                pair = new DOM.Element(input, 
                    nameQuotesType: nameQuotesType,
                    nameInterval: nameInterval,
                    delimiter: delimiter,
                    delimiterInterval: delimiterInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(delimiter)
                );
            }
            else if (nameText.StartsWith("@"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.E;
                pair = new DOM.Attribute(input,
                    nameInterval: nameInterval,
                    delimiter: delimiter,
                    delimiterInterval: delimiterInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(delimiter)
                );
            }
            else if (nameText.StartsWith("!$"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;
                pair = new DOM.AliasDefinition(input,
                    nameInterval: nameInterval,
                    delimiter: delimiter,
                    delimiterInterval: delimiterInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(delimiter)
                );
            }
            else if (nameText.StartsWith("!#"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.EE;
                pair = new DOM.NamespaceDefinition(input,
                    nameInterval: nameInterval,
                    delimiter: delimiter,
                    delimiterInterval: delimiterInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(delimiter)
                );
            }
            else if (nameText.StartsWith("!%"))
            {
                pair = new DOM.Parameter(input,
                    nameInterval: nameInterval,
                    delimiter: delimiter,
                    delimiterInterval: delimiterInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(delimiter)
                );
            }
            else if (nameText.StartsWith("!"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;
                pair = new DOM.Document(input,
                    nameInterval: nameInterval,
                    delimiter: delimiter,
                    delimiterInterval: delimiterInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(delimiter)
                );
            }
            else if (nameText.StartsWith("$"))
            {
                pair = new DOM.Alias(input,
                    nameInterval: nameInterval,
                    delimiter: delimiter,
                    delimiterInterval: delimiterInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(delimiter)
                );
            }
            else if (nameText.StartsWith("%"))
            {
                pair = new DOM.Argument(input,
                    nameInterval: nameInterval,
                    delimiter: delimiter,
                    delimiterInterval: delimiterInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(delimiter)
                );
            }
            else if (nameText.StartsWith("#"))
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;
                pair = new DOM.Scope(input,
                    nameInterval: nameInterval,
                    delimiter: delimiter,
                    delimiterInterval: delimiterInterval,
                    valueType: GetValueType(delimiter)
                );
            }
            else
            {
                if (delimiter == DelimiterEnum.None)
                    delimiter = DelimiterEnum.C;

                pair = new DOM.Element(input,
                    nameQuotesType: nameQuotesType,
                    nameInterval: nameInterval,
                    delimiter: delimiter,
                    delimiterInterval: delimiterInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent, 
                    valueType: GetValueType(delimiter) 
                    
                );
            }
            return (Pair) pair;
        }

        private ValueType GetValueType(DelimiterEnum delimiter)
        {
            switch (delimiter)
            {
                case DelimiterEnum.CE:
                    return ValueType.PairValue;

                case DelimiterEnum.EC:
                    return ValueType.Concatenation;

                case DelimiterEnum.ECC:
                    return ValueType.LiteralChoice;
                case DelimiterEnum.C:
                case DelimiterEnum.CC:
                    return ValueType.Object;
            }
            return ValueType.None;
        }

        internal static string GetNameText(ITextSource input, int nameQuotesType, Interval nameInterval)
        {
            if (nameQuotesType == 0)
                return input.GetText(nameInterval.Begin.Index, nameInterval.End.Index);
            var c = input.GetChar(nameInterval.End.Index);
            if (nameQuotesType == 1)
                return c == '\''
                    ? input.GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index - 1)
                    : input.GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index);

            return c == '"'
                ? input.GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index - 1)
                : input.GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index);
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

        public Syntactik.DOM.Comment ProcessComment(ITextSource input, int commentType, Interval commentInterval)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return null;
        }
    }
}