using System;
using System.Collections.Generic;
using System.Threading;
using Syntactik.Compiler;
using Syntactik.Compiler.IO;
using Syntactik.DOM;
using Syntactik.IO;
using Syntactik.MonoDevelop.Completion.DOM;
using Mapped = Syntactik.DOM.Mapped;
using Syntactik.MonoDevelop.Parser;
using AliasDefinition = Syntactik.DOM.AliasDefinition;
using Document = Syntactik.DOM.Document;
using Module = Syntactik.DOM.Module;

namespace Syntactik.MonoDevelop.Completion
{
    public class CompletionContext
    {
        private readonly CancellationToken _cancellationToken;
        private CompilerContext _context;
        private int _offset;
        private Func<Dictionary<string, AliasDefinition>> _aliasDefinitions;
        public SortedSet<CompletionExpectation> Expectations { get; }
        public CompletionExpectation InTag { get; private set; }
        public Pair LastPair { get; private set; }

        public int Offset => _offset;

        private CompletionContext(CancellationToken cancellationToken = new CancellationToken())
        {
            Expectations = new SortedSet<CompletionExpectation>();
            _cancellationToken = cancellationToken;
        }

        public static CompletionContext CreateCompletionContext(string fileName, string text, int offset,
            Func<Dictionary<string, AliasDefinition>> aliasDefinitions,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var result = new CompletionContext(cancellationToken);
            result.Parse(fileName, text, offset, aliasDefinitions);
            return result;
        }

        protected void Parse(string fileName, string text, int offset, Func<Dictionary<string, AliasDefinition>> aliasDefinitions)
        {
            _offset = offset - 1;
            _aliasDefinitions = aliasDefinitions;
            InputStream input;
            var compilerParameters = CreateCompilerParametersForCompletion(fileName, text, _offset, _cancellationToken, out input);
            var compiler = new SyntactikCompiler(compilerParameters);
            _context = compiler.Run();
            object lastPair;
            if (_context.InMemoryOutputObjects.TryGetValue("LastPair", out lastPair))
                LastPair = (Pair)lastPair;
            StoreValues(_context);
            input.Dispose();
        }

        private bool _expectationsCalculated;
        public void CalculateExpectations()
        {
            if (_expectationsCalculated) return;
            _expectationsCalculated = true;
            InTag = CompletionExpectation.NoExpectation;
            if (LastPair == null) //Module
            {
                var module = _context.CompileUnit.Modules[0];
                LastPair = module;
                if (module.Members.Count == 0 && module.ModuleDocument == null)
                    AddExpectation(CompletionExpectation.NamespaceDefinition);
                AddExpectation(CompletionExpectation.Alias);
                AddExpectation(CompletionExpectation.Element);
                AddExpectation(CompletionExpectation.Document);
                AddExpectation(CompletionExpectation.AliasDefinition);
                return;
            }

            var alias = LastPair as Mapped.Alias;
            if (alias != null)
            {
                if (alias.NameInterval.End.Index >= _offset)
                {
                    InTag = CompletionExpectation.Alias;
                    AddExpectation(CompletionExpectation.Alias);
                    return;
                }

                if (alias.Delimiter == DelimiterEnum.None) return;
                if (alias.Delimiter == DelimiterEnum.E || alias.Delimiter == DelimiterEnum.EE)
                {
                    AddExpectation(CompletionExpectation.Value);
                    return;
                }
                if (alias.Delimiter == DelimiterEnum.C)
                {
                    AliasDefinition aliasDef1; 
                    if (_aliasDefinitions().TryGetValue(alias.Name, out aliasDef1) && ((Mapped.AliasDefinition)aliasDef1).Parameters.Count > 0)
                        AddExpectation(CompletionExpectation.Argument);
                }
                return;
            }
            var argument = LastPair as Mapped.Argument;
            if (argument != null)
            {
                if (argument.NameInterval.End.Index >= _offset)
                {
                    InTag = CompletionExpectation.Argument;
                    AddExpectation(CompletionExpectation.Argument);
                    return;
                }
                if (argument.Delimiter == DelimiterEnum.None) return;
                if (argument.Delimiter == DelimiterEnum.E || argument.Delimiter == DelimiterEnum.EE)
                {
                    AddExpectation(CompletionExpectation.Value);
                    return;
                }
                if (argument.Delimiter == DelimiterEnum.C)
                {
                    AddExpectation(CompletionExpectation.Alias);
                    AddExpectation(CompletionExpectation.Element);
                    AddExpectation(CompletionExpectation.Attribute);
                }
                return;
            }
            var attribute = LastPair as Mapped.Attribute;
            if (attribute != null)
            {
                InTag = CompletionExpectation.Attribute;
                if (attribute.NameInterval.End.Index >= _offset)
                {
                    AddExpectation(CompletionExpectation.Attribute);
                    return;
                }

                if (attribute.Delimiter == DelimiterEnum.E || attribute.Delimiter == DelimiterEnum.EE)
                {
                    AddExpectation(CompletionExpectation.Value);
                    return;
                }
                return;
            }

            var element = LastPair as Mapped.Element;
            if (element != null)
            {
                if (element.NameInterval.End.Index >= _offset)
                {
                    InTag = CompletionExpectation.Element;
                    AddExpectation(CompletionExpectation.Element);
                    return;
                }
                if (element.Delimiter == DelimiterEnum.None) return;
                if (element.Delimiter == DelimiterEnum.E || element.Delimiter == DelimiterEnum.EE)
                {
                    AddExpectation(CompletionExpectation.Value);
                    return;
                }
                if (element.Delimiter == DelimiterEnum.C)
                {
                    AddExpectation(CompletionExpectation.Alias);
                    AddExpectation(CompletionExpectation.Element);
                    AddExpectation(CompletionExpectation.Attribute);
                }
                return;
            }

            var document = LastPair as Mapped.Document;
            if (document != null)
            {
                if (document.NameInterval.End.Index >= _offset)
                {
                    InTag = CompletionExpectation.Document;
                    return;
                }
                if (document.Delimiter == DelimiterEnum.None) return;
                if (document.Delimiter == DelimiterEnum.E || document.Delimiter == DelimiterEnum.EE)
                {
                    AddExpectation(CompletionExpectation.Value);
                    return;
                }
                if (document.Delimiter == DelimiterEnum.C)
                {
                    AddExpectation(CompletionExpectation.Alias);
                    AddExpectation(CompletionExpectation.Element);
                }
                return;
            }
            var nsDef = LastPair as Mapped.NamespaceDefinition;
            if (nsDef != null)
            {
                InTag = CompletionExpectation.NamespaceDefinition;
                if (nsDef.Delimiter == DelimiterEnum.E || nsDef.Delimiter == DelimiterEnum.EE)
                {
                    AddExpectation(CompletionExpectation.Value);
                }
                return;
            }

            var aliasDef = LastPair as Mapped.AliasDefinition;
            if (aliasDef != null)
            {
                if (aliasDef.NameInterval.End.Index >= _offset)
                {
                    InTag = CompletionExpectation.AliasDefinition;
                    return;
                }
                if (aliasDef.Delimiter == DelimiterEnum.None) return;
                if (aliasDef.Delimiter == DelimiterEnum.E || aliasDef.Delimiter == DelimiterEnum.EE)
                {
                    AddExpectation(CompletionExpectation.Value);
                    return;
                }
                if (aliasDef.Delimiter == DelimiterEnum.C)
                {
                    AddExpectation(CompletionExpectation.Attribute);
                    AddExpectation(CompletionExpectation.Alias);
                    AddExpectation(CompletionExpectation.Element);
                }
                return;
            }
        }

        /// <summary>
        /// Going through dom tree reading names and ns. It causes storing of names localy in pair objects.
        /// After that we can dispose input object which keeping the whole editor text string in the memory.
        /// </summary>
        /// <param name="context"></param>
        private void StoreValues(CompilerContext context)
        {
            var visitor = new CompletionVisitor();
            visitor.Visit(context.CompileUnit.Modules[0]);
            //Calling StoreStringValues separately for LastPair for the case when it couldn't be added to he parent node, because of error
            (LastPair as ICompletionNode)?.StoreStringValues(); 
        }

        private void AddExpectation(CompletionExpectation expectation)
        {
            Expectations.Add(expectation);
        }

        private static CompilerParameters CreateCompilerParametersForCompletion(string fileName, string content, 
            int offset, CancellationToken cancellationToken, out InputStream input)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            input = new InputStream(content, GetOffset(offset, content)); //This is performance hack to prevent copying of editors content.
            compilerParameters.Pipeline.Steps.Add(new ParseForCompletionStep(cancellationToken, input));
                
            compilerParameters.Input.Add(new StringInput(fileName, content)); //This input is not really used. This is hack to prevent copying of editors content
            return compilerParameters;
        }

        private static int GetOffset(int offset, string content)
        {
            var length = content.Length;
            if (offset >= length) return length;
            offset++;
            var startValue = offset;
            var nonSpaceMet = false;
            while (offset < length && !IntegerCharExtensions.IsEndOfOpenName(content[offset]))
            {
                if (content[offset] != ' ' && content[offset] != '\t')
                    nonSpaceMet = true;
                offset++;
            }
            return nonSpaceMet?offset:startValue;
        }

        public IEnumerable<Pair> GetPath()
        {
            var pair = LastPair;
            var list = new List<Pair>();
            var document = pair as Document;
            if (document != null && (document.Parent as Module)?.ModuleDocument == document) return list;

            while (pair != null)
            {
                list.Add(pair);
                pair = pair.Parent;
            }
            //list.Reverse();
            return list;
        }
    }

    public enum CompletionExpectation
    {
        NoExpectation,
        Alias,
        AliasDefinition,
        Argument,
        Attribute,
        Document,
        Element,
        NamespaceDefinition,
        Value
        //NoExpectation = -1,
        //Alias = 3,
        //AliasDefinition = 7,
        //Argument = 4,
        //Attribute = 1,
        //Document = 6,
        //Element = 2,
        //NamespaceDefinition = 0,
        //Value = 5,
    }
}