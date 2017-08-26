using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MonoDevelop.Core.Text;
using Syntactik.Compiler;
using Syntactik.Compiler.Generator;
using Syntactik.Compiler.Steps;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Alias = Syntactik.DOM.Alias;
using Document = Syntactik.DOM.Mapped.Document;
using Element = Syntactik.DOM.Element;
using Parameter = Syntactik.DOM.Mapped.Parameter;

namespace Syntactik.MonoDevelop.Commands
{
    internal class GenerateXmlForSelectionVisitor : AliasResolvingVisitor
    {
        private readonly XmlWriter _xmlTextWriter;
        private readonly ISegment _selectionRange;
        private readonly Stack<ChoiceInfo> _choiceStack = new Stack<ChoiceInfo>();

        public GenerateXmlForSelectionVisitor(XmlWriter xmlTextWriter, CompilerContext context, ISegment selectionRange)
            : base(context)
        {
            _xmlTextWriter = xmlTextWriter;
            _selectionRange = selectionRange;
        }

        public override void OnDocument(DOM.Document document)
        {
            _currentDocument = (Document) document;
            _choiceStack.Push(_currentDocument.ChoiceInfo);
            base.OnDocument(document);
            _currentDocument = null;
        }

        private bool EnterChoiceContainer(DOM.Mapped.Alias alias, PairCollection<Entity> entities)
        {
            if (alias.AliasDefinition.Delimiter != DelimiterEnum.CC &&
                alias.AliasDefinition.Delimiter != DelimiterEnum.ECC
                || entities == null || entities.Count == 0)
                return false;

            var choice = _choiceStack.Peek();
            var choiceInfo = FindChoiceInfo(choice, alias);
            if (choice.ChoiceNode != alias)
            {
                _choiceStack.Push(choiceInfo);
            }
            _choiceStack.Push(choiceInfo.Children[0]);
            if (((Element) choiceInfo.Children[0].ChoiceNode).Entities.Count > 0)
                Visit(((Element) choiceInfo.Children[0].ChoiceNode).Entities);

            _choiceStack.Pop();
            if (choice.ChoiceNode != alias)
                _choiceStack.Pop();
            return true;
        }

        internal static ChoiceInfo FindChoiceInfo(ChoiceInfo choice, Pair pair)
        {
            if (choice.ChoiceNode == pair) return choice;
            if (choice.Children != null)
                foreach (var child in choice.Children)
                {
                    if (child.ChoiceNode == pair) return child;
                }
            return null;
        }

        public override void OnElement(Element pair)
        {
            var inSelection = IsInSelection(pair as IMappedPair, _selectionRange);
            if (inSelection)
            {
                string prefix, ns;
                NamespaceResolver.GetPrefixAndNs(pair, _currentDocument,
                    () => ScopeContext.Peek(),
                    out prefix, out ns);
                
                //Starting Element
                if (!string.IsNullOrEmpty(pair.Name)) //not text node
                    _xmlTextWriter.WriteStartElement(prefix == null ? pair.Name : $"{prefix}:{pair.Name}");
                ResolveValue(pair);
            }

            ResolveAttributesInSelection(pair.Entities);
            Visit(pair.Entities.Where(e => !(e is DOM.Attribute)));

            if (inSelection)
            {
                //End Element
                if (!string.IsNullOrEmpty(pair.Name)) //not text node
                    _xmlTextWriter.WriteEndElement();
            }
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

            return pair.DelimiterInterval.Begin.Index >= selectionRange.Offset &&
                   pair.DelimiterInterval.Begin.Index <= selectionRange.EndOffset ||
                   pair.DelimiterInterval.End.Index >= selectionRange.Offset &&
                   pair.DelimiterInterval.End.Index <= selectionRange.EndOffset;
        }

        public override void OnAlias(DOM.Alias alias)
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
                AliasContext.Push(new AliasContext()
                {
                    AliasDefinition = aliasDef,
                    Alias = (DOM.Mapped.Alias) alias,
                    AliasNsInfo = GetContextNsInfo()
                });
                if (!EnterChoiceContainer((DOM.Mapped.Alias) alias, aliasDef.Entities))
                    Visit(aliasDef.Entities.Where(e => !(e is DOM.Attribute)));
                AliasContext.Pop();
            }
            else
            {
                Visit(alias.Entities);
            }
        }

        public override void OnValue(string value, ValueType type)
        {
            _xmlTextWriter.WriteString(value);
        }

        protected override void ResolveSqsEscape(EscapeMatch escapeMatch, StringBuilder sb)
        {
            char c = ResolveSqsEscapeChar(escapeMatch);
            if (XmlGenerator.IsLegalXmlChar(c))
            {
                sb.Append(c);
            }
        }

        public override void OnAttribute(DOM.Attribute pair)
        {
            var inSelection = IsInSelection(pair as IMappedPair, _selectionRange);

            if (!inSelection || _xmlTextWriter.WriteState != WriteState.Element) return;

            string prefix, ns;

            NamespaceResolver.GetPrefixAndNs(pair, _currentDocument,
                () => ScopeContext.Peek(),
                out prefix, out ns);
            _xmlTextWriter.WriteStartAttribute(prefix == null?pair.Name:$"{prefix}:{pair.Name}");
            ResolveValue(pair);
            _xmlTextWriter.WriteEndAttribute();
        }
    }
}