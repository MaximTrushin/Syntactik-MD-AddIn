using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Core.Collections;
using Syntactik.Compiler;
using Syntactik.Compiler.IO;
using Syntactik.DOM;
using Syntactik.MonoDevelop.Completion.DOM;
using Syntactik.MonoDevelop.Parser;

namespace Syntactik.MonoDevelop.Completion
{
    public class CompletionContext
    {
        private readonly string _fileName;
        private readonly string _text;
        private readonly int _offset;
        private readonly CancellationToken _cancellationToken;
        public Set<CompletionExpectation> Expectations { get; }
        public CompletionExpectation InTag { get; private set; }

        public CompletionContext(string fileName, string text, int offset, CancellationToken cancellationToken = new CancellationToken())
        {
            Expectations = new Set<CompletionExpectation>();
            _fileName = fileName;
            _text = text;
            _offset = offset;
            _cancellationToken = cancellationToken;
        }

        public void CalculateExpectations()
        {
            var compilerParameters = CreateCompilerParameters(_fileName, _text, _offset);
            var compiler = new SyntactikCompiler(compilerParameters);
            var context = compiler.Run();
            InTag = CompletionExpectation.NoExpectation;

            var visitor = new CompletionContextVisitor();
            visitor.Visit(context.CompileUnit);
            LastPair = visitor.LastPair;

            if (LastPair == null) //Empty module
            {
                AddExpectation(CompletionExpectation.Namespace);
                AddExpectation(CompletionExpectation.Alias);
                AddExpectation(CompletionExpectation.Element);
                AddExpectation(CompletionExpectation.Attribute);
            }

            var alias = LastPair as Syntactik.DOM.Mapped.Alias;
            if (alias != null)
            {
                if (alias.NameInterval.End.Index == _offset)
                    InTag = CompletionExpectation.Alias;
                AddExpectation(CompletionExpectation.Alias);
            }
        }

        public Pair LastPair { get; private set; }

        private void AddExpectation(CompletionExpectation expectation)
        {
            Expectations.Add(expectation);
        }

        private CompilerParameters CreateCompilerParameters(string fileName, string content, int offset
            )
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
        Element,
        Argument,
        Namespace,
        Attribute,
        Value
    }
}