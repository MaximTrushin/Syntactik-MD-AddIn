using System.Threading;
using Syntactik.Compiler.Steps;
using Syntactik.DOM.Mapped;
using Syntactik.IO;

namespace Syntactik.MonoDevelop.Parser
{
    public class ParseAndCreateFolding : Parse
    {
        private readonly SyntactikParsedDocument _parsedDocument;
        private readonly CancellationToken _cancellationToken;

        public ParseAndCreateFolding(SyntactikParsedDocument parsedDocument, CancellationToken cancellationToken)
        {
            _parsedDocument = parsedDocument;
            _cancellationToken = cancellationToken;
        }

        protected override Syntactik.Parser GetParser(Module module, ICharStream input)
        {
            return module.TargetFormat == Module.TargetFormats.Json ? new Syntactik.Parser(input, new FoldingReportingPairFactory(new ReportingPairFactoryForJson(_context, module), _parsedDocument, _cancellationToken), module) : 
                new Syntactik.Parser(input, new FoldingReportingPairFactory(new ReportingPairFactoryForXml(_context, module), _parsedDocument, _cancellationToken), module);
        }
    }
}
