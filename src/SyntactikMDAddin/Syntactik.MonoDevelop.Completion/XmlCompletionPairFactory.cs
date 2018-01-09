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
            AssignmentEnum assignment,
            Interval assignmentInterval, int valueQuotesType, Interval valueInterval, int valueIndent)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            IMappedPair pair;
            var nameText = GetNameText(input, nameQuotesType, nameInterval);
            if (nameQuotesType > 0)
            {
                if (assignment == AssignmentEnum.None)
                    assignment = AssignmentEnum.C;

                pair = new DOM.Element(input, 
                    nameQuotesType: nameQuotesType,
                    nameInterval: nameInterval,
                    assignment: assignment,
                    assignmentInterval: assignmentInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(assignment)
                );
            }
            else if (nameText.StartsWith("@"))
            {
                if (assignment == AssignmentEnum.None)
                    assignment = AssignmentEnum.E;
                pair = new DOM.Attribute(input,
                    nameInterval: nameInterval,
                    assignment: assignment,
                    assignmentInterval: assignmentInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(assignment)
                );
            }
            else if (nameText.StartsWith("!$"))
            {
                if (assignment == AssignmentEnum.None)
                    assignment = AssignmentEnum.C;
                pair = new DOM.AliasDefinition(input,
                    nameInterval: nameInterval,
                    assignment: assignment,
                    assignmentInterval: assignmentInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(assignment)
                );
            }
            else if (nameText.StartsWith("!#"))
            {
                if (assignment == AssignmentEnum.None)
                    assignment = AssignmentEnum.EE;
                pair = new DOM.NamespaceDefinition(input,
                    nameInterval: nameInterval,
                    assignment: assignment,
                    assignmentInterval: assignmentInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(assignment)
                );
            }
            else if (nameText.StartsWith("!%"))
            {
                pair = new DOM.Parameter(input,
                    nameInterval: nameInterval,
                    assignment: assignment,
                    assignmentInterval: assignmentInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(assignment)
                );
            }
            else if (nameText.StartsWith("!"))
            {
                if (assignment == AssignmentEnum.None)
                    assignment = AssignmentEnum.C;
                pair = new DOM.Document(input,
                    nameInterval: nameInterval,
                    assignment: assignment,
                    assignmentInterval: assignmentInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(assignment)
                );
            }
            else if (nameText.StartsWith("$"))
            {
                pair = new DOM.Alias(input,
                    nameInterval: nameInterval,
                    assignment: assignment,
                    assignmentInterval: assignmentInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(assignment)
                );
            }
            else if (nameText.StartsWith("%"))
            {
                pair = new DOM.Argument(input,
                    nameInterval: nameInterval,
                    assignment: assignment,
                    assignmentInterval: assignmentInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent,
                    valueType: GetValueType(assignment)
                );
            }
            else if (nameText.StartsWith("#"))
            {
                if (assignment == AssignmentEnum.None)
                    assignment = AssignmentEnum.C;
                pair = new DOM.Scope(input,
                    nameInterval: nameInterval,
                    assignment: assignment,
                    assignmentInterval: assignmentInterval,
                    valueType: GetValueType(assignment)
                );
            }
            else
            {
                if (assignment == AssignmentEnum.None)
                    assignment = AssignmentEnum.C;

                pair = new DOM.Element(input,
                    nameQuotesType: nameQuotesType,
                    nameInterval: nameInterval,
                    assignment: assignment,
                    assignmentInterval: assignmentInterval,
                    valueQuotesType: valueQuotesType,
                    valueInterval: valueInterval,
                    valueIndent: valueIndent, 
                    valueType: GetValueType(assignment) 
                    
                );
            }
            return (Pair) pair;
        }

        private ValueType GetValueType(AssignmentEnum assignment)
        {
            switch (assignment)
            {
                case AssignmentEnum.CE:
                    return ValueType.PairValue;

                case AssignmentEnum.EC:
                    return ValueType.Concatenation;

                case AssignmentEnum.ECC:
                    return ValueType.LiteralChoice;
                case AssignmentEnum.C:
                case AssignmentEnum.CC:
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