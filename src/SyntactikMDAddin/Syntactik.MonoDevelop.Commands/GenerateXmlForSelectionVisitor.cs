using System.Collections.Generic;
using System.Linq;
using System.Xml;
using MonoDevelop.Core.Text;
using Syntactik.Compiler;
using Syntactik.Compiler.Generator;
using Syntactik.DOM.Mapped;
using Alias = Syntactik.DOM.Alias;
using Document = Syntactik.DOM.Mapped.Document;
using Element = Syntactik.DOM.Element;
using Parameter = Syntactik.DOM.Mapped.Parameter;

namespace Syntactik.MonoDevelop.Commands
{
    internal class GenerateXmlForSelectionVisitor : XmlGenerator
    {
        private readonly ISegment _selectionRange;

        public GenerateXmlForSelectionVisitor(XmlWriter xmlTextWriter, CompilerContext context, ISegment selectionRange)
            : base((name, encoding) => xmlTextWriter, null, context)
        {
            XmlTextWriter = xmlTextWriter;
            _selectionRange = selectionRange;
        }

        public override void Visit(DOM.Document document)
        {
            CurrentDocument = (Document) document;
            ChoiceStack.Push(CurrentDocument.ChoiceInfo);
            Visit(document.Entities);
            CurrentDocument = null;
        }

        public override void Visit(Element pair)
        {
            var inSelection = IsInSelection(pair as IMappedPair, _selectionRange);
            if (inSelection)
            {
                string prefix = null, ns;
                if (CurrentDocument != null) //User can copy from AliasDef
                    NamespaceResolver.GetPrefixAndNs(pair, CurrentDocument,
                        ScopeContext.Peek(),
                        out prefix, out ns);
                
                //Starting Element
                if (!string.IsNullOrEmpty(pair.Name)) //not text node
                    XmlTextWriter.WriteStartElement(prefix == null ? pair.Name : $"{prefix}:{pair.Name}");
                ResolveValue(pair);
            }

            ResolveAttributesInSelection(pair.Entities);
            Visit(pair.Entities.Where(e => !(e is DOM.Attribute)));

            if (inSelection)
            {
                //End Element
                if (!string.IsNullOrEmpty(pair.Name)) //not text node
                    XmlTextWriter.WriteEndElement();
            }
        }

        public override void Visit(DOM.AliasDefinition aliasDef)
        {
            Visit(aliasDef.Entities);        
        }

        /// <summary>
        /// Go through all entities and resolve attributes for the current node.
        /// </summary>
        /// <param name="entities">List of entities. Looking for alias or parameter because they potentially can hold the attributes.</param>
        protected void ResolveAttributesInSelection(IEnumerable<DOM.Entity> entities)
        {
            foreach (var entity in entities)
            {
                if (entity is DOM.Mapped.Attribute)
                {
                    Visit(entity);
                }
                else if (entity is Alias && IsInSelection(entity as IMappedPair, _selectionRange))
                {
                    ResolveAttributesInAlias(entity as DOM.Mapped.Alias);
                }
                else if (entity is Parameter && IsInSelection(entity as IMappedPair, _selectionRange))
                {
                    ResolveAttributesInParameter(entity as Parameter);
                }
            }
        }

        private bool IsInSelection(IMappedPair pair, ISegment selectionRange)
        {
            if (AliasContext.Count > 1) return true;

            if (pair == null) return false;

            if (pair.NameInterval != null)
            {
                return pair.NameInterval.Begin.Index >= selectionRange.Offset &&
                       pair.NameInterval.Begin.Index <= selectionRange.EndOffset ||
                       pair.NameInterval.End.Index >= selectionRange.Offset &&
                       pair.NameInterval.End.Index <= selectionRange.EndOffset;
            }

            return pair.AssignmentInterval.Begin.Index >= selectionRange.Offset &&
                   pair.AssignmentInterval.Begin.Index <= selectionRange.EndOffset ||
                   pair.AssignmentInterval.End.Index >= selectionRange.Offset &&
                   pair.AssignmentInterval.End.Index <= selectionRange.EndOffset;
        }

        public override void Visit(Alias alias)
        {
            var inSelection = IsInSelection(alias as IMappedPair, _selectionRange);

            if (inSelection)
            {
                var aliasDef = ((DOM.Mapped.Alias) alias).AliasDefinition;
                if (aliasDef.IsValueNode)
                {
                    ValueType valueType;
                    OnValue(ResolveValueAlias((DOM.Mapped.Alias) alias, out valueType), valueType);
                }
                AliasContext.Push((DOM.Mapped.Alias) alias);
                if (!EnterChoiceContainer((DOM.Mapped.Alias) alias, aliasDef.Entities))
                    Visit(aliasDef.Entities.Where(e => !(e is DOM.Attribute)));
                AliasContext.Pop();
            }
            else
            {
                Visit(alias.Entities);
                Visit(alias.Arguments);
            }
        }



        public override void Visit(DOM.Attribute pair)
        {
            var inSelection = IsInSelection(pair as IMappedPair, _selectionRange);

            if (!inSelection || XmlTextWriter.WriteState != WriteState.Element) return;

            string prefix, ns;

            NamespaceResolver.GetPrefixAndNs(pair, CurrentDocument,
                ScopeContext.Peek(),
                out prefix, out ns);
            XmlTextWriter.WriteStartAttribute(prefix == null?pair.Name:$"{prefix}:{pair.Name}");
            ResolveValue(pair);
            XmlTextWriter.WriteEndAttribute();
        }
    }
}