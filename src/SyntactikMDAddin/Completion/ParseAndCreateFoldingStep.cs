using System;
using System.Threading;
using Syntactik.Compiler.Steps;
using Syntactik.DOM;
using Syntactik.IO;
using Syntactik.MonoDevelop.Completion;

namespace Syntactik.MonoDevelop.Parser
{
    public class ParseForCompletionStep : Parse
    {
        private readonly CancellationToken _cancellationToken;

        public ParseForCompletionStep(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        protected override Syntactik.Parser GetParser(Module module, ICharStream input)
        {
            var m = module as DOM.Mapped.Module;
            if (m == null) throw new ArgumentException("Invalid module type.");
            return m.TargetFormat == DOM.Mapped.Module.TargetFormats.Json ? new Syntactik.Parser(input, new JsonCompletionPairFactory(_context, module,_cancellationToken), module) : 
                new Syntactik.Parser(input, new XmlCompletionPairFactory(_context, module, _cancellationToken), module);
        }
    }
}
