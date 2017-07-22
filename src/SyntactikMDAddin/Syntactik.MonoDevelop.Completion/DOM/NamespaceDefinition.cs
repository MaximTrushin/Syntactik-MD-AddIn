using System.Collections.Generic;
using Syntactik.DOM;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    public class NamespaceDefinition : Syntactik.DOM.Mapped.NamespaceDefinition, ICompletionNode
    {
        private readonly ICharStream _input;

        internal NamespaceDefinition(ICharStream input)
        {
            _input = input;
        }
        public override string Name
        {
            get
            {
                if (base.Name != null) return base.Name;
                base.Name = Element.GetNameText(_input, NameQuotesType, NameInterval).Substring(2);
                return base.Name;
            }
            set { base.Name = value; }
        }

        public override string Value
        {
            get
            {
                if (base.Value != null) return base.Value;
                base.Value = Element.GetNameText(_input, ValueQuotesType, ValueInterval);
                return base.Value;
            }
            set { base.Value = value; }
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
            InterpolationItems = null;
        }
    }
}
