using System;
using System.Threading;
using Syntactik.Compiler.Steps;
using Syntactik.DOM;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Parser
{
    class ParseAndCreateFoldingStep : Parse
    {
        private readonly SyntactikParsedDocument _parsedDocument;
        private readonly CancellationToken _cancellationToken;

        public ParseAndCreateFoldingStep(SyntactikParsedDocument parsedDocument, CancellationToken cancellationToken)
        {
            _parsedDocument = parsedDocument;
            _cancellationToken = cancellationToken;
        }

        protected override Syntactik.Parser GetParser(Module module, ICharStream input)
        {
            var m = module as DOM.Mapped.Module;
            if (m == null) throw new ArgumentException("Invalid module type.");

            return m.TargetFormat == DOM.Mapped.Module.TargetFormats.Json ? new Syntactik.Parser(input, new FoldingReportingPairFactory(new ReportingPairFactoryForJson(_context, module), _parsedDocument, _cancellationToken), module) : 
                new Syntactik.Parser(input, new FoldingReportingPairFactory(new ReportingPairFactoryForXml(_context, module), _parsedDocument, _cancellationToken), module);
        }
    }
}
