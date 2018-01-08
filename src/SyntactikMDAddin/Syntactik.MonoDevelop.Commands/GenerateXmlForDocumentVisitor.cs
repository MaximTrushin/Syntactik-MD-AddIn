using System;
using System.Text;
using System.Xml;
using Syntactik.Compiler;
using Syntactik.Compiler.Generator;
using Syntactik.DOM.Mapped;
using Document = Syntactik.DOM.Document;

namespace Syntactik.MonoDevelop.Commands
{
    internal class GenerateXmlForDocumentVisitor : XmlGenerator, IDisposable
    {
        public GenerateXmlForDocumentVisitor(Func<string, Encoding, XmlWriter> writerDelegate, CompilerContext context)
            : base(writerDelegate, null, context, true)
        {
        }

        public override void Visit(Document document)
        {
            CurrentDocument = (DOM.Mapped.Document)document;
            _currentModuleMember = document;
            _choiceStack.Push(CurrentDocument.ChoiceInfo);
            var encoding = GetEncoding(document);
            _xmlTextWriter = _writerDelegate(document.Name, encoding);

            WriteStartDocument(document);
            _rootElementAdded = false;
            Visit(document.Entities);
            _xmlTextWriter.WriteEndDocument();
            _xmlTextWriter.Flush();
            CurrentDocument = null;
            _currentModuleMember = null;
        }

        protected override void AddLocationMapRecord(string fileName, IMappedPair pair)
        {
        }

        public void Dispose()
        {
            _xmlTextWriter.Dispose();
        }
    }
}
