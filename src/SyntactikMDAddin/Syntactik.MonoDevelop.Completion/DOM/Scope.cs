using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    class Scope : Syntactik.DOM.Mapped.Element, ICompletionNode
    {
        private readonly ITextSource _input;

        internal Scope(ITextSource input, AssignmentEnum assignment = AssignmentEnum.None, Interval nameInterval = null, Interval valueInterval = null, Interval assignmentInterval = null,
            int nameQuotesType = 0, int valueQuotesType = 0, int valueIndent = 0, ValueType valueType = ValueType.None) : base(nameInterval: nameInterval, valueInterval: valueInterval,
            assignmentInterval: assignmentInterval, nameQuotesType: nameQuotesType, valueQuotesType: valueQuotesType, assignment: assignment, valueIndent: valueIndent, valueType: valueType)
        {
            _input = input;
        }
        private string _name;
        public override string Name => _name ?? (_name = Element.GetNameText(_input, NameQuotesType, NameInterval).Substring(1));

        private Pair _lastAddedChild;
        public override void AppendChild(Pair child)
        {
            var completionNode = _lastAddedChild as ICompletionNode;
            completionNode?.DeleteChildren();
            _lastAddedChild = child;
            base.AppendChild(child);
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
