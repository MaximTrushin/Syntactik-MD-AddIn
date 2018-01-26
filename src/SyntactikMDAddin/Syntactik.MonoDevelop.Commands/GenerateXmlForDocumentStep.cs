using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Policies;
using Syntactik.Compiler;
using Syntactik.DOM;
using MonoDevelop.Xml.Formatting;

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
                var provider = PolicyService.GetUserDefaultPolicySet() as IPolicyProvider;
                var policyContainr = provider.Policies;
                var mimeTypeScopes = DesktopService.GetMimeTypeInheritanceChain("application/xml").ToList();
                var policy = policyContainr.Get<XmlFormattingPolicy>(mimeTypeScopes);
                _context.InMemoryOutputObjects = new Dictionary<string, object>();

                using (var visitor =
                    new GenerateXmlForDocumentVisitor(
                        (name, encoding) =>
                            XmlWriter.Create(_memoryStream, new XmlWriterSettings
                            {
                                Encoding = encoding,
                                ConformanceLevel = ConformanceLevel.Document,
                                Indent = policy.DefaultFormat.IndentContent,
                                NamespaceHandling = NamespaceHandling.OmitDuplicates,
                                NewLineHandling = NewLineHandling.None,
                                NewLineChars = policy.DefaultFormat.NewLineChars,
                                IndentChars = policy.DefaultFormat.ContentIndentString
                            })
                            , _compilerContext))
                {
                    var module = _compilerContext.CompileUnit.Modules.First(m => m.FileName == _doc.Module.FileName);
                    var doc = module.Members.First(m => m.Name == _doc.Name);
                    visitor.Visit(doc);
                    _memoryStream.Position = 0;
                    _context.InMemoryOutputObjects["CLIPBOARD"] = new StreamReader(_memoryStream).ReadToEnd();
                }
            }
            catch (Exception)
            {
                //ignoring Generate XML exception.
            }
        }
    }
}
