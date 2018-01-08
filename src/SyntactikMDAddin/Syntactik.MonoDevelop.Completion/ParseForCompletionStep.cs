﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Syntactik.Compiler;
using Syntactik.Compiler.Steps;
using Syntactik.DOM;
using Syntactik.IO;
using Syntactik.MonoDevelop.Completion;

namespace Syntactik.MonoDevelop.Parser
{
    class ParseForCompletionStep : Parse
    {
        private readonly CancellationToken _cancellationToken;
        private readonly ICharStream _input;

        public ParseForCompletionStep(CancellationToken cancellationToken, ICharStream input)
        {
            _cancellationToken = cancellationToken;
            _input = input;
        }

        protected override Syntactik.Parser GetParser(Module module, ICharStream input)
        {
            var m = module as DOM.Mapped.Module;
            if (m == null) throw new ArgumentException("Invalid module type.");

            return m.TargetFormat == DOM.Mapped.Module.TargetFormats.Json ? new Syntactik.Parser(input, new JsonCompletionPairFactory(_context, module,_cancellationToken), module) : 
                new Syntactik.Parser(input, new XmlCompletionPairFactory(_context, module, _cancellationToken), module);
        }

        protected override void DoParse(string fileName, TextReader reader)
        {
            _context.InMemoryOutputObjects = new Dictionary<string, object>();
            try
            {
                var module = CreateModule(fileName);
                _context.CompileUnit.AppendChild(module);
                Syntactik.Parser parser = GetParser(module, _input);
                var errorListener = new ErrorListener(_context, fileName);
                parser.ErrorListeners.Add(errorListener);
                parser.ParseModule();
            }
            catch (Exception ex)
            {
                _context.AddError(CompilerErrorFactory.FatalError(ex));
            }

        }
    }
}
