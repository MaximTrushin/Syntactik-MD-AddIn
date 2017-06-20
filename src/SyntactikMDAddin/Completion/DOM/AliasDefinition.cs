using System;
using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    public class AliasDefinition: Syntactik.DOM.Mapped.AliasDefinition
    {
        private Entity _entity;

        public override void AppendChild(Pair child)
        {
            var ns = child as NamespaceDefinition;
            if (ns != null)
            {
                NamespaceDefinitions.Add(ns);
                return;
            }

            var entity = child as Entity;
            if (entity != null)
            {
                _entities = null;
                _entity = entity;
                return;
            }
            base.AppendChild(child);
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
    }
}
