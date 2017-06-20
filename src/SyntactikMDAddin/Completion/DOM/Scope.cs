using System;
using System.Collections.Generic;
using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    public class Scope : Syntactik.DOM.Mapped.Element
    {
        private Entity _entity;
        public override void AppendChild(Pair child)
        {
            if (Delimiter == DelimiterEnum.EC)
            {
                InterpolationItems = new List<object> { child };
                child.InitializeParent(this);
                return;
            }
            var item = child as Entity;
            if (item != null)
            {
                _entity = item;
            }
            else
            {
                base.AppendChild(child);
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
    }
}
