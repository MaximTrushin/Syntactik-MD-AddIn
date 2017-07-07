using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    public class Argument : Syntactik.DOM.Mapped.Argument
    {
        private Entity _entity;

        public override void AppendChild(Pair child)
        {
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
