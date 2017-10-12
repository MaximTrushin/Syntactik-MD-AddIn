using System;
using System.Text;
using System.Xml;
using Syntactik.Compiler;
using Syntactik.Compiler.Generator;
using Syntactik.DOM.Mapped;
using Document = Syntactik.DOM.Document;

namespace Syntactik.MonoDevelop.Commands
{
    internal class GenerateXmlForDocumentVisitor : XmlGenerator
    {
        public GenerateXmlForDocumentVisitor(Func<string, Encoding, XmlWriter> writerDelegate, CompilerContext context)
            : base(writerDelegate, null, context)
        {
        }

        public override void OnDocument(Document document)
        {
            _currentDocument = (DOM.Mapped.Document)document;
            _currentModuleMember = document;
            _choiceStack.Push(_currentDocument.ChoiceInfo);
            var encoding = GetEncoding(document);
            using (_xmlTextWriter = _writerDelegate(document.Name, encoding))
            {
                _xmlTextWriter.WriteStartDocument();
                _rootElementAdded = false;
                Visit(document.Entities);
                _xmlTextWriter.WriteEndDocument();
                _xmlTextWriter.Flush();
            }

            _currentDocument = null;
            _currentModuleMember = null;
        }

        protected override void AddLocationMapRecord(string fileName, IMappedPair pair)
        {
        }
    }
}
