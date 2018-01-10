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
            CurrentModuleMember = document;
            ChoiceStack.Push(CurrentDocument.ChoiceInfo);
            var encoding = GetEncoding(document);
            XmlTextWriter = WriterDelegate(document.Name, encoding);

            WriteStartDocument(document);
            DocumentElementAdded = false;
            Visit(document.Entities);
            XmlTextWriter.WriteEndDocument();
            XmlTextWriter.Flush();
            CurrentDocument = null;
            CurrentModuleMember = null;
        }

        protected override void AddLocationMapRecord(string fileName, IMappedPair pair)
        {
        }

        public void Dispose()
        {
            XmlTextWriter.Dispose();
        }
    }
}
