using System;
using System.Collections.Generic;
using Syntactik.DOM;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    public class Element: Syntactik.DOM.Mapped.Element, ICompletionNode
    {
        private readonly ICharStream _input;

        internal Element(ICharStream input)
        {
            _input = input;
        }
        private Entity _entity;
        public override void AppendChild(Pair child)
        {
            if (Delimiter == DelimiterEnum.EC)
            {
                child.InitializeParent(this);
                InterpolationItems = new List<object> {child};
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

        public override string Name
        {
            get
            {
                if (base.Name != null) return base.Name;
                var nameText = GetNameText(_input, NameQuotesType, NameInterval);
                if (NameQuotesType > 0)
                {
                    base.Name = nameText;
                    return nameText;
                }
                var tuple = GetNameAndNs(nameText, NameQuotesType);
                var ns = string.IsNullOrEmpty(tuple.Item1) ? null : tuple.Item1;
                base.Name = tuple.Item2;
                NsPrefix = ns;
                return base.Name;

            }
            set { base.Name = value; }
        }

        public override string NsPrefix
        {
            get
            {
                if (!string.IsNullOrEmpty(base.Name)) return base.Name;
                var nameText = GetNameText(_input, NameQuotesType, NameInterval);
                if (NameQuotesType > 0)
                {
                    base.Name = nameText;
                    return "";
                }
                var tuple = GetNameAndNs(nameText, NameQuotesType);
                var ns = string.IsNullOrEmpty(tuple.Item1) ? null : tuple.Item1;
                base.Name = tuple.Item2;
                base.NsPrefix = ns;
                return base.NsPrefix;

            }
            set { base.Name = value; }
        }

        internal static string GetNameText(ICharStream input, int nameQuotesType, Interval nameInterval)
        {
            if (nameQuotesType == 0)
                return ((ITextSource)input).GetText(nameInterval.Begin.Index, nameInterval.End.Index);
            var c = ((ITextSource)input).GetChar(nameInterval.End.Index);
            if (nameQuotesType == 1)
                return c == '\'' ? ((ITextSource)input).GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index - 1) : ((ITextSource)input).GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index);

            return c == '"' ? ((ITextSource)input).GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index - 1) : ((ITextSource)input).GetText(nameInterval.Begin.Index + 1, nameInterval.End.Index);
        }

        public void StoreStringValues()
        {
            if (Name != null)
            {
            }
        }

    }
}
