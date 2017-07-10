using System;
using Syntactik.DOM;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    public class AliasDefinition: Syntactik.DOM.Mapped.AliasDefinition, ICompletionNode
    {
        private Entity _entity;
        private readonly ICharStream _input;

        internal AliasDefinition(ICharStream input)
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

        public override void AppendChild(Pair child)
        {
            var ns = child as NamespaceDefinition;
            if (ns != null)
            {
                child.InitializeParent(this);
                NamespaceDefinitions.Add(ns);
                return;
            }

            var entity = child as Entity;
            if (entity != null)
            {
                child.InitializeParent(this);
                _entities = null;
                _entity = entity;
                return;
            }
        }

        public override PairCollection<Entity> Entities
        {
            get
            {
                if (_entities != null) return _entities;
                _entities = new PairCollection<Entity>(this);
                if (_entity != null) _entities.Add(_entity);
                return _entities;
            }
            set { throw new NotImplementedException(); }
        }
        public void StoreStringValues()
        {
            if (Name != null)
            {
            }
        }
    }
}
