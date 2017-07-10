using System;
using Syntactik.DOM;
using NamespaceDefinition = Syntactik.DOM.Mapped.NamespaceDefinition;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    /// <summary>
    /// Module DOM-class for completion.
    /// It can have zero or many Namespace definitions.
    /// It also can have either one module member or entity.
    /// </summary>
    public class Module: Syntactik.DOM.Mapped.Module
    {
        private ModuleMember _member;
        private Entity _entity;

        public override void AppendChild(Pair child)
        {
            if (child is NamespaceDefinition)
            {
                base.AppendChild(child);
                return;
            }

            var member = child as ModuleMember;
            if (member != null)
            {
                child.InitializeParent(this);
                _entity = null;
                _members = null;
                _moduleDocument = null;
                _member = member;
                return;
            }

            var entity = child as Entity;
            if (entity != null)
            {
                child.InitializeParent(this);
                _moduleDocument = null;
                _members = null;
                _member = null;
                _entity = entity;
            }
        }

        public override Syntactik.DOM.Document ModuleDocument
        {
            get
            {
                if (_moduleDocument != null) return _moduleDocument;
                if (_entity != null)
                {
                    _moduleDocument = new Syntactik.DOM.Mapped.Document
                    {
                        Name = Name,
                        NameInterval = Interval.Empty,
                        Delimiter = DelimiterEnum.C
                    };
                    _moduleDocument.AppendChild(_entity);
                }

                return _moduleDocument;
            }
            set { throw new NotImplementedException(); }
        }

        public override PairCollection<ModuleMember> Members
        {
            get
            {
                if (_members != null) return _members;
                _members = new PairCollection<ModuleMember>(this);
                if (_member != null) _members.Add(_member);
                return _members;
            }
            set { throw new NotImplementedException(); }
        }
    }
}
