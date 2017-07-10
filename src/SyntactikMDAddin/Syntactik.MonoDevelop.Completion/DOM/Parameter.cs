using System;
using System.Collections.Generic;
using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    public class Parameter : Syntactik.DOM.Mapped.Parameter, ICompletionNode
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
                child.InitializeParent(this);
                _entity = item;
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
        }
    }
}
