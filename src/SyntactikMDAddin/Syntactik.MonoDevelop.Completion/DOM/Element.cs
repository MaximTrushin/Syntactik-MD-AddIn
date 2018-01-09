using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    class Element: Syntactik.DOM.Mapped.Element, ICompletionNode
    {
        private readonly ITextSource _input;

        internal Element(ITextSource input, AssignmentEnum assignment = AssignmentEnum.None, Interval nameInterval = null, Interval valueInterval = null, Interval assignmentInterval = null,
            int nameQuotesType = 0, int valueQuotesType = 0, int valueIndent = 0, ValueType valueType = ValueType.None) :base(nameInterval: nameInterval, valueInterval: valueInterval, 
                assignmentInterval: assignmentInterval, nameQuotesType: nameQuotesType, valueQuotesType: valueQuotesType, assignment: assignment, valueIndent: valueIndent,
                valueType: valueType)
        {
            _input = input;
        }

        private Pair _lastAddedChild;
        public override void AppendChild(Pair child)
        {
            var completionNode = _lastAddedChild as ICompletionNode;
            completionNode?.DeleteChildren();
            _lastAddedChild = child;
            base.AppendChild(child);
        }

        private string _name;
        public override string Name
        {
            get
            {
                if (_name != null) return _name;
                var nameText = GetNameText(_input, NameQuotesType, NameInterval);
                if (NameQuotesType > 0)
                {
                    return nameText;
                }
                var tuple = GetNameAndNs(nameText, NameQuotesType);
                var ns = string.IsNullOrEmpty(tuple.Item1) ? null : tuple.Item1;
                OverrideNsPrefix(ns);
                return _name = tuple.Item2;

            }
        }

        internal static string GetNameText(ITextSource input, int nameQuotesType, Interval nameInterval)
        {
            if (nameQuotesType == 0)
                return input.GetText(nameInterval.Begin.Index, nameInterval.End.Index);
            var c = input.GetChar(nameInterval.End.Index);
            if (nameQuotesType == 1)
                return c == '\'' ? input.GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index - 1) : input.GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index);

            return c == '"' ? input.GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index - 1) : input.GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index);
        }

        public void StoreStringValues()
        {
            if (Name != null)
            {
            }
        }

        public void DeleteChildren()
        {
            InterpolationItems?.Clear();
            Entities = null;
        }
    }
}
