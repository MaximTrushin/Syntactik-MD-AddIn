using System;
using Syntactik.DOM;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    public class Alias: Syntactik.DOM.Mapped.Alias, ICompletionNode
    {
        private Entity _entity;
        private Argument _argument;
        private readonly ICharStream _input;

        internal Alias(ICharStream input)
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
            child.InitializeParent(this);
            var item = child as Argument;
            if (item != null)
            {
                child.InitializeParent(this);
                _entity = null;
                _entities = null;
                _argument = item;
                return;
            }
            var entity = child as Entity;
            if (entity != null)
            {
                child.InitializeParent(this);
                _argument = null;
                _arguments = null;
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

        public void StoreStringValues()
        {
            if (Name != null)
            {
            }
        }
    }
}
