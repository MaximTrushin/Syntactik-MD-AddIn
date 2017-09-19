using System.Linq;
using MonoDevelop.Core.Text;
using Newtonsoft.Json;
using Syntactik.Compiler;
using Syntactik.Compiler.Generator;
using Syntactik.DOM;
using Syntactik.DOM.Mapped;
using Alias = Syntactik.DOM.Alias;
using Element = Syntactik.DOM.Element;

namespace Syntactik.MonoDevelop.Commands
{
    internal class GenerateJsonForSelectionVisitor : JsonGenerator
    {
        private readonly ISegment _selectionRange;

        public GenerateJsonForSelectionVisitor(JsonWriter jsonTextWriter, CompilerContext context, ISegment selectionRange)
            : base(name => jsonTextWriter, context)
        {
            _selectionRange = selectionRange;
        }

        private void CheckBlockStart(Pair node, bool inSelection)
        {
            if (!_blockStart || !inSelection) return;

            //This element is the first element of the block. It decides if the block is array or object
            if (string.IsNullOrEmpty(node.Name) || node.Delimiter == DelimiterEnum.None)
            {
                _jsonWriter.WriteStartArray(); //start array
                
                _blockState.Push(BlockState.Array);
            }
            else
            {
                _jsonWriter.WriteStartObject(); //start array
                _blockState.Push(BlockState.Object);
            }
            _blockStart = false;
        }

        public override void OnElement(Element element)
        {
            var inSelection = IsInSelection(element as IMappedPair, _selectionRange);
            CheckBlockStart(element, inSelection);
            if (inSelection)
            {
                if (!string.IsNullOrEmpty(element.Name) && element.Delimiter != DelimiterEnum.None)
                    _jsonWriter.WritePropertyName((element.NsPrefix != null ? element.NsPrefix + "." : "") + element.Name);

                if (ResolveValue(element)) return; //Block has value therefore it has no block.
            }

            //Working with node's block
            if (inSelection) _blockStart = true;
            var prevBlockStateCount = _blockState.Count;
            Visit(element.Entities);

            if (inSelection) _blockStart = false;

            if (inSelection && _blockState.Count > prevBlockStateCount)
            {
                if (_blockState.Pop() == BlockState.Array)
                {
                    _jsonWriter.WriteEndArray();
                }
                else
                {
                    _jsonWriter.WriteEndObject();
                }
                return;
            }

            //Element hase nor block no value. Writing an empty object as a value.
            if (inSelection && (!string.IsNullOrEmpty(element.Name) || ((DOM.Mapped.Element)element).ValueType == ValueType.Object))
            {
                if (element.Delimiter == DelimiterEnum.CC)
                {
                    _jsonWriter.WriteStartArray();
                    _jsonWriter.WriteEndArray();
                }
                else
                {
                    _jsonWriter.WriteStartObject();
                    _jsonWriter.WriteEndObject();
                }
            }
        }

        public override void OnAliasDefinition(DOM.AliasDefinition aliasDef)
        {
            Visit(aliasDef.Entities);
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

        public override void OnAlias(Alias alias)
        {
            var inSelection = IsInSelection(alias as IMappedPair, _selectionRange);

            if (inSelection)
            {
                var aliasDef = ((DOM.Mapped.Alias)alias).AliasDefinition;
                if (aliasDef.IsValueNode)
                {
                    CheckBlockStartForAlias();
                    ValueType valueType;
                    OnValue(ResolveValueAlias((DOM.Mapped.Alias)alias, out valueType), valueType);
                }

                AliasContext.Push(new AliasContext() { AliasDefinition = aliasDef, Alias = (DOM.Mapped.Alias)alias, AliasNsInfo = GetContextNsInfo() });
                if (!EnterChoiceContainer((DOM.Mapped.Alias)alias, aliasDef.Entities))
                    Visit(aliasDef.Entities.Where(e => !(e is DOM.Attribute)));
                AliasContext.Pop();
            }
            else
            {
                Visit(alias.Entities);
                Visit(alias.Arguments);
            }
        }
    }
}