using System.Collections.Generic;
using Syntactik.DOM;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    public class Attribute : Syntactik.DOM.Mapped.Attribute, ICompletionNode
    {
        private readonly ICharStream _input;

        internal Attribute(ICharStream input)
        {
            _input = input;
        }
        public override string Name
        {
            get
            {
                if (base.Name != null) return base.Name;
                var nameText = Element.GetNameText(_input, NameQuotesType, NameInterval).Substring(1);
                var tuple = Syntactik.DOM.Mapped.Element.GetNameAndNs(nameText, NameQuotesType);
                var ns = string.IsNullOrEmpty(tuple.Item1) ? null : tuple.Item1;
                base.Name = tuple.Item2;
                NsPrefix = ns;
                return base.Name;
            }
            set { base.Name = value; }
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
            if (Name != null)
            {
            }
        }

        public void DeleteChildren()
        {
            InterpolationItems = null;
        }
    }
}
