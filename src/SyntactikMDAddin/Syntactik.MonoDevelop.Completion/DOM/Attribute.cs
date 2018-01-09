using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    class Attribute : Syntactik.DOM.Mapped.Attribute, ICompletionNode
    {
        private readonly ITextSource _input;

        internal Attribute(ITextSource input, AssignmentEnum assignment = AssignmentEnum.None, Interval nameInterval = null, Interval valueInterval = null, Interval assignmentInterval = null,
            int nameQuotesType = 0, int valueQuotesType = 0, int valueIndent = 0, ValueType valueType = ValueType.None) : base(nameInterval: nameInterval, valueInterval: valueInterval,
            assignmentInterval: assignmentInterval, nameQuotesType: nameQuotesType, valueQuotesType: valueQuotesType, assignment: assignment, valueIndent: valueIndent, valueType: valueType)
        {
            _input = input;
        }

        private string _name;
        public override string Name
        {
            get
            {
                if (_name != null) return _name;
                var nameText = Element.GetNameText(_input, NameQuotesType, NameInterval).Substring(1);
                var tuple = Syntactik.DOM.Mapped.Element.GetNameAndNs(nameText, NameQuotesType);
                var ns = string.IsNullOrEmpty(tuple.Item1) ? null : tuple.Item1;
                OverrideNsPrefix(ns);
                return _name = tuple.Item2;
            }
        }

        private string _value;
        public override string Value
        {
            get
            {
                if (_value != null) return _value;
                if (ValueInterval == null) return string.Empty;
                return _value = Element.GetNameText(_input, ValueQuotesType, ValueInterval); 
            }
        }

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
            if (Name != null){}
            if (Value != null){}
        }

        public void DeleteChildren()
        {
            InterpolationItems?.Clear();
        }
    }
}
