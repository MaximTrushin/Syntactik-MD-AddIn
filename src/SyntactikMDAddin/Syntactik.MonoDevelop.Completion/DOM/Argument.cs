using System;
using Syntactik.DOM;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    public class Argument : Syntactik.DOM.Mapped.Argument, ICompletionNode
    {
        private Entity _entity;
        private readonly ICharStream _input;

        internal Argument(ICharStream input)
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

        public override void AppendChild(Pair child)
        {
            var item = child as Entity;
            if (item != null)
            {
                _entity = item;
                child.InitializeParent(this);
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
