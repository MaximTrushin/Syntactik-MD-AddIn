using System;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using Newtonsoft.Json;
using Syntactik.Compiler;

namespace Syntactik.MonoDevelop.Commands
{
    class GenerateJsonForSelectionStep : ICompilerStep
    {
        private readonly CompilerContext _compilerContext;
        private readonly ISegment _selectionRange;
        private CompilerContext _context;
        private readonly StringWriter _stringWriter;

        public GenerateJsonForSelectionStep(CompilerContext compilerContext, ISegment selectionRange)
        {
            _compilerContext = compilerContext;
            _selectionRange = selectionRange;
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
                var module = _context.CompileUnit.Modules[0];
                using (var jsonWriter = new JsonTextWriter(_stringWriter) {Formatting = Newtonsoft.Json.Formatting.Indented})
                {
                    var visitor = new GenerateJsonForSelectionVisitor(jsonWriter, _compilerContext, _selectionRange);
                    visitor.Visit(module);
                }
                _context.InMemoryOutputObjects["CLIPBOARD"] = _stringWriter.ToString();
            }
            catch (JsonWriterException) { }
            catch (Exception ex)
            {
                LoggingService.LogError("Unhandled exception in GenerateJsonForSelectionStep.Run.", ex);
            }
        }
    }
}
