using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using Syntactik.Compiler;

namespace Syntactik.MonoDevelop.Commands
{
    class GenerateXmlForSelectionStep : ICompilerStep
    {
        private readonly CompilerContext _compilerContext;
        private readonly ISegment _selectionRange;
        private CompilerContext _context;
        private readonly StringWriter _stringWriter;

        public GenerateXmlForSelectionStep(CompilerContext compilerContext, ISegment selectionRange)
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
                using (var xmlWriter = new XmlTextWriter(_stringWriter) {Formatting = System.Xml.Formatting.Indented})
                {
                    var visitor = new GenerateXmlForSelectionVisitor(xmlWriter, _compilerContext, _selectionRange);
                    visitor.Visit(module);
                }
                _context.InMemoryOutputObjects["CLIPBOARD"] = _stringWriter.ToString();
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Unhandled exception in GenerateXmlForSelectionStep.Run.", ex);
            }
        }
    }
}
