using System;
using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    public class Alias: Syntactik.DOM.Mapped.Alias
    {
        private Entity _entity;
        private Argument _argument;
        public override void AppendChild(Pair child)
        {
            var item = child as Argument;
            if (item != null)
            {
                _entity = null;
                _entities = null;
                _argument = item;
                return;
            }
            var entity = child as Entity;
            if (entity != null)
            {
                _argument = null;
                _arguments = null;
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
        public override PairCollection<Syntactik.DOM.Argument> Arguments
        {
            get
            {
                if (_arguments != null) return _arguments;
                _arguments = new PairCollection<Syntactik.DOM.Argument>(this);
                if (_argument != null) _arguments.Add(_argument);
                return _arguments;
            }
            set { throw new NotImplementedException(); }
        }
    }
}
