using System;
using System.Collections.Generic;
using System.Threading;
using Syntactik.Compiler;
using Syntactik.Compiler.IO;
using Syntactik.Compiler.Steps;
using Syntactik.DOM;
using Mapped = Syntactik.DOM.Mapped;
using Syntactik.MonoDevelop.Parser;

namespace Syntactik.MonoDevelop.Completion
{
    public class CompletionContext
    {
        private readonly string _fileName;
        private readonly string _text;
        private readonly int _offset;
        private readonly Func<Dictionary<string, AliasDefinition>> _aliasDefinitions;
        private readonly CancellationToken _cancellationToken;
        public SortedSet<CompletionExpectation> Expectations { get; }
        public CompletionExpectation InTag { get; private set; }
        public Pair LastPair { get; private set; }

        public CompletionContext(string fileName, string text, int offset, Func<Dictionary<string, AliasDefinition>> aliasDefinitions, CancellationToken cancellationToken = new CancellationToken())
        {
            Expectations = new SortedSet<CompletionExpectation>();
            _fileName = fileName;
            _text = text;
            _offset = offset - 1;
            _aliasDefinitions = aliasDefinitions;
            _cancellationToken = cancellationToken;
        }

        public void CalculateExpectations()
        {
            var compilerParameters = CreateCompilerParameters(_fileName, _text, _offset);
            var compiler = new SyntactikCompiler(compilerParameters);
            var context = compiler.Run();
            InTag = CompletionExpectation.NoExpectation;

            //var visitor = new CompletionContextVisitor();
            //visitor.Visit(context.CompileUnit);
            object lastPair;
            if (context.InMemoryOutputObjects.TryGetValue("LastPair", out lastPair))
                LastPair = (Pair) lastPair;

            if (LastPair == null) //Module
            {
                var module = context.CompileUnit.Modules[0];
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
        }

        private void AddExpectation(CompletionExpectation expectation)
        {
            Expectations.Add(expectation);
        }

        private CompilerParameters CreateCompilerParameters(string fileName, string content, int offset)
        {
            var compilerParameters = new CompilerParameters { Pipeline = new CompilerPipeline() };
            compilerParameters.Pipeline.Steps.Add(new ParseForCompletionStep(_cancellationToken));
            compilerParameters.Input.Add(new StringInput(fileName, content.Substring(0, offset < content.Length?offset + 1: content.Length)));
            return compilerParameters;
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