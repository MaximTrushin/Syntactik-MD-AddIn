using System;
using System.Collections.Generic;
using System.Threading;
using Syntactik.Compiler;
using Syntactik.Compiler.IO;
using Syntactik.DOM;
using Syntactik.IO;
using Mapped = Syntactik.DOM.Mapped;
using Syntactik.MonoDevelop.Parser;
using AliasDefinition = Syntactik.DOM.AliasDefinition;

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
            StoreValues(_context);
            input.Dispose();
            object lastPair;
            if (_context.InMemoryOutputObjects.TryGetValue("LastPair", out lastPair))
                LastPair = (Pair)lastPair;
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
                    AddExpectation(CompletionExpectation.Namespace);
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
                    AliasDefinition aliasDef; 
                    if (_aliasDefinitions().TryGetValue(alias.Name, out aliasDef) && ((Mapped.AliasDefinition)aliasDef).Parameters.Count > 0)
                        AddExpectation(CompletionExpectation.Argument);
                    return;
                }
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
                    return;
                }
            }

            var document = LastPair as Mapped.Document;
            if (document != null)
            {
                if (document.NameInterval.End.Index >= _offset)
                {
                    InTag = CompletionExpectation.Document;
                    AddExpectation(CompletionExpectation.Element);
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
                    return;
                }
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
            while (offset < length && !IntegerCharExtensions.IsEndOfOpenName(content[offset]))
            {
                offset++;
            }
            return offset;
        }

        public IEnumerable<Pair> GetPath()
        {
            var pair = LastPair;
            var list = new List<Pair>();
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
        Namespace,
        Value,
    }
}