using System.Xml;
using Syntactik.Compiler;
using Syntactik.Compiler.Generator;
using Syntactik.DOM.Mapped;
using Document = Syntactik.DOM.Document;

namespace Syntactik.MonoDevelop.Commands
{
    internal class GenerateXmlForDocumentVisitor : XmlGenerator
    {
        public GenerateXmlForDocumentVisitor(XmlTextWriter xmlWriter, CompilerContext context)
            : base(name => xmlWriter, null, context)
        {
            _xmlTextWriter = xmlWriter;
        }

        public override void OnDocument(Document document)
        {
            _currentDocument = (DOM.Mapped.Document)document;
            _choiceStack.Push(_currentDocument.ChoiceInfo);
            _xmlTextWriter.WriteStartDocument();
            _rootElementAdded = false;
            Visit(document.Entities);
            _xmlTextWriter.WriteEndDocument();
            _currentDocument = null;
        }

        protected override void AddLocationMapRecord(string fileName, IMappedPair pair)
        {
        }
    }
}
