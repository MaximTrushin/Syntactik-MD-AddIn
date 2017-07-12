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

        public CompletionContext(CancellationToken cancellationToken = new CancellationToken())
        {
            Expectations = new SortedSet<CompletionExpectation>();
            _cancellationToken = cancellationToken;
        }

        public void Parse(string fileName, string text, int offset, Func<Dictionary<string, AliasDefinition>> aliasDefinitions)
        {
            _offset = offset - 1;
            _aliasDefinitions = aliasDefinitions;
            InputStream input;
            var compilerParameters = CreateCompilerParametersForCompletion(fileName, text, offset, out input);
            var compiler = new SyntactikCompiler(compilerParameters);
            _context = compiler.Run();
            StoreValues(_context);
            input.Dispose();
            object lastPair;
            if (_context.InMemoryOutputObjects.TryGetValue("LastPair", out lastPair))
                LastPair = (Pair)lastPair;
        }

        public void CalculateExpectations()
        {
            InTag = CompletionExpectation.NoExpectation;
            if (LastPair == null) //Module
            {
                var module = _context.CompileUnit.Modules[0];
                if (module.Members.Count == 0 && module.ModuleDocument == null)
                    AddExpectation(CompletionExpectation.Namespace);
                AddExpectation(CompletionExpectation.Alias);
                AddExpectation(CompletionExpectation.Element);
                AddExpectation(CompletionExpectation.Attribute);
                AddExpectation(CompletionExpectation.Document);
                AddExpectation(CompletionExpectation.AliasDefinition);
                return;
            }

            var alias = LastPair as Mapped.Alias;
            if (alias != null)
            {
                
                if (alias.NameInterval.End.Index == _offset)
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
                if (argument.NameInterval.End.Index == _offset)
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

        private CompilerParameters CreateCompilerParametersForCompletion(string fileName, string content, int offset, out InputStream input)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            input = new InputStream(content, offset < content.Length ? offset + 1 : content.Length); //This is performance hack to prevent copying of editors content.
            compilerParameters.Pipeline.Steps.Add(new ParseForCompletionStep(_cancellationToken, input));
                
            compilerParameters.Input.Add(new StringInput(fileName, content)); //This input is not really used. This is hack to prevent copying of editors content
            return compilerParameters;
        }

        internal class CompletionVisitor : SyntactikDepthFirstVisitor
        {
            protected override void OnPair(Pair pair)
            {
                var cn = pair as ICompletionNode;
                cn?.StoreStringValues();
                base.OnPair(pair);
            }
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