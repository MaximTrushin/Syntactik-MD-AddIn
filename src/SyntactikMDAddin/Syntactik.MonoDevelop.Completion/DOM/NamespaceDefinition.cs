using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    class NamespaceDefinition : Syntactik.DOM.Mapped.NamespaceDefinition, ICompletionNode
    {
        private readonly ITextSource _input;

        internal NamespaceDefinition(ITextSource input, DelimiterEnum delimiter = DelimiterEnum.None, Interval nameInterval = null, Interval valueInterval = null, 
            Interval delimiterInterval = null, int nameQuotesType = 0, int valueQuotesType = 0, int valueIndent = 0, ValueType valueType = ValueType.None) : base(
                nameInterval: nameInterval, valueInterval: valueInterval, delimiterInterval: delimiterInterval, nameQuotesType: nameQuotesType, 
                valueQuotesType: valueQuotesType, delimiter: delimiter, valueIndent: valueIndent, valueType: valueType)
        {
            _input = input;
        }

        private string _name;
        public override string Name => _name?? (_name = Element.GetNameText(_input, NameQuotesType, NameInterval).Substring(2));

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
