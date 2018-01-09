using System;
using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    /// <summary>
    /// Module DOM-class for completion.
    /// It can have zero or many NamespaceDefinitions.
    /// It also can have either one module member or entity.
    /// </summary>
    class Module: Syntactik.DOM.Mapped.Module
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
                Members = null;
                //_moduleDocument = null;
                _member = member;
                return;
            }

            var entity = child as Entity;
            if (entity != null)
            {
                child.InitializeParent(this);
                //_moduleDocument = null;
                Members = null;
                _member = null;
                _entity = entity;
            }
        }

        public override Syntactik.DOM.Document ModuleDocument
        {
            get
            {
                if (base.ModuleDocument != null) return base.ModuleDocument;
                if (_entity != null)
                {
                    //_moduleDocument = new Syntactik.DOM.Mapped.Document
                    //(
                    //    Name,
                    //    nameInterval: Interval.Empty,
                    //    assignment: AssignmentEnum.C
                    //);
                    //_moduleDocument.AppendChild(_entity);
                    AddEntity(_entity);
                }
                return base.ModuleDocument;
            }
        }

        public override PairCollection<ModuleMember> Members
        {
            get
            {
                if (base.Members != null) return base.Members;
                base.Members = new PairCollection<ModuleMember>(this);
                if (_member != null) base.Members.Add(_member);
                return base.Members;
            }
            set { base.Members = value; }
        }

        public Module(string name, string fileName = null) : base(name, fileName)
        {
        }
    }
}
