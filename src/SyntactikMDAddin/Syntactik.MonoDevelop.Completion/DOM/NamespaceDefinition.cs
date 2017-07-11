﻿using System.Collections.Generic;
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
                base.Name = Element.GetNameText(_input, NameQuotesType, NameInterval).Substring(1);
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