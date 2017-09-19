using System;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using Newtonsoft.Json;
using Syntactik.Compiler;
using Syntactik.Compiler.Generator;
using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Commands
{
    class GenerateJsonForDocumentStep : ICompilerStep
    {
        private readonly CompilerContext _compilerContext;
        private CompilerContext _context;
        private readonly Document _doc;
        private readonly StringWriter _stringWriter;

        public GenerateJsonForDocumentStep(CompilerContext compilerContext, Document doc)
        {
            _compilerContext = compilerContext;
            _doc = doc;
            _stringWriter = new StringWriter();
        }

        public void Initialize(CompilerContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            _context = null;
        }

        public void Run()
        {
            try
            {
                _context.InMemoryOutputObjects = new Dictionary<string, object>();
                using (var jsonWriter = new JsonTextWriter(_stringWriter) { Formatting = Newtonsoft.Json.Formatting.Indented })
                {
                    var writer = jsonWriter;
                    var visitor = new JsonGenerator(name => writer, _compilerContext);
                    visitor.Visit(_doc);
                    _context.InMemoryOutputObjects["CLIPBOARD"] = _stringWriter.ToString();
                }
            }
            catch (JsonWriterException) { }
            catch (Exception ex)
            {
                LoggingService.LogError("Unhandled exception in GenerateJsonForDocumentStep.Run.", ex);
            }
        }
    }
}
