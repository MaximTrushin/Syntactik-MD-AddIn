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
            if (!BlockIsStarting || !inSelection) return;

            //This element is the first element of the block. It decides if the block is array or object
            if (string.IsNullOrEmpty(node.Name) || node.Assignment == AssignmentEnum.None)
            {
                JsonWriter.WriteStartArray(); //start array
                
                BlockState.Push(BlockStateEnum.Array);
            }
            else
            {
                JsonWriter.WriteStartObject(); //start array
                BlockState.Push(BlockStateEnum.Object);
            }
            BlockIsStarting = false;
        }

        public override void Visit(Element element)
        {
            var inSelection = IsInSelection(element as IMappedPair, _selectionRange);
            CheckBlockStart(element, inSelection);
            if (inSelection)
            {
                if (!string.IsNullOrEmpty(element.Name) && element.Assignment != AssignmentEnum.None)
                    JsonWriter.WritePropertyName((element.NsPrefix != null ? element.NsPrefix + "." : "") + element.Name);

                if (ResolveValue(element)) return; //Block has value therefore it has no block.
            }

            //Working with node's block
            if (inSelection) BlockIsStarting = true;
            var prevBlockStateCount = BlockState.Count;
            Visit(element.Entities);

            if (inSelection) BlockIsStarting = false;

            if (inSelection && BlockState.Count > prevBlockStateCount)
            {
                if (BlockState.Pop() == BlockStateEnum.Array)
                {
                    JsonWriter.WriteEndArray();
                }
                else
                {
                    JsonWriter.WriteEndObject();
                }
                return;
            }

            //Element has nor block no value. Writing an empty object as a value.
            if (inSelection && (!string.IsNullOrEmpty(element.Name) || ((DOM.Mapped.Element)element).ValueType == ValueType.Object))
            {
                if (element.Assignment == AssignmentEnum.CC)
                {
                    JsonWriter.WriteStartArray();
                    JsonWriter.WriteEndArray();
                }
                else
                {
                    JsonWriter.WriteStartObject();
                    JsonWriter.WriteEndObject();
                }
            }
        }

        public override void Visit(DOM.AliasDefinition aliasDef)
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
                var aliasDef = ((DOM.Mapped.Alias)alias).AliasDefinition;
                if (aliasDef.IsValueNode)
                {
                    CheckBlockStartForAlias();
                    ValueType valueType;
                    OnValue(ResolveValueAlias((DOM.Mapped.Alias)alias, out valueType), valueType);
                }

                AliasContext.Push((DOM.Mapped.Alias)alias);
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