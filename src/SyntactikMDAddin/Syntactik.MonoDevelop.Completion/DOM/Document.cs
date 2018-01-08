using System;
using System.Collections.Generic;
using Syntactik.DOM;
using Syntactik.IO;
using ValueType = Syntactik.DOM.Mapped.ValueType;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    /// <summary>
    /// Document DOM-class for completion.
    /// It can have zero or many Namespace definitions.
    /// It also can have either one entity.
    /// </summary>
    class Document: Syntactik.DOM.Mapped.Document, ICompletionNode
    {
        private readonly ITextSource _input;

        internal Document(ITextSource input, DelimiterEnum delimiter = DelimiterEnum.None, Interval nameInterval = null, Interval valueInterval = null, Interval delimiterInterval = null,
            int nameQuotesType = 0, int valueQuotesType = 0, int valueIndent = 0, ValueType valueType = ValueType.None) : base(nameInterval: nameInterval, valueInterval: valueInterval,
            delimiterInterval: delimiterInterval, nameQuotesType: nameQuotesType, valueQuotesType: valueQuotesType, delimiter: delimiter, valueIndent: valueIndent,
            valueType: valueType)
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
