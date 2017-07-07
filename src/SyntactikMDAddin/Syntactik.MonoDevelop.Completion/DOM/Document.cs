using System;
using System.Collections.Generic;
using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    /// <summary>
    /// Document DOM-class for completion.
    /// It can have zero or many Namespace definitions.
    /// It also can have either one entity.
    /// </summary>
    public class Document: Syntactik.DOM.Mapped.Document
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
                DocumentElement = entity;
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
