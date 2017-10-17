using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using MonoDevelop.Core;
using Syntactik.Compiler;
using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Commands
{
    class GenerateXmlForDocumentStep : ICompilerStep
    {
        private readonly CompilerContext _compilerContext;
        private CompilerContext _context;
        private readonly MemoryStream _memoryStream;
        private readonly Document _doc;

        public GenerateXmlForDocumentStep(CompilerContext compilerContext, Document doc)
        {
            _compilerContext = compilerContext;
            _doc = doc;
            _memoryStream = new MemoryStream();
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

                using (var visitor =
                    new GenerateXmlForDocumentVisitor(
                        (name, encoding) =>
                            new XmlTextWriter(_memoryStream, encoding)
                            {
                                Formatting = System.Xml.Formatting.Indented
                            }, _compilerContext))
                {

                    visitor.Visit(_doc);
                    //_memoryStream.
                    _memoryStream.Position = 0;
                    _context.InMemoryOutputObjects["CLIPBOARD"] = new StreamReader(_memoryStream).ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Unhandled exception in GenerateXmlForDocumentStep.Run.", ex);
            }
        }
    }
}
