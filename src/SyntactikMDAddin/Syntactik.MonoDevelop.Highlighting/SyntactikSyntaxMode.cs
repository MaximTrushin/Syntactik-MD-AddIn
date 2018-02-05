using System.Collections.Generic;
using System.Linq;
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using Syntactik.DOM.Mapped;

namespace Syntactik.MonoDevelop.Highlighting
{
    class SyntactikSyntaxMode : SyntaxMode
    {
        private readonly Module.TargetFormats _targetFormat;

        public SyntactikSyntaxMode(TextDocument doc) : base(doc)
        {
            _targetFormat = doc.MimeType == "text/x-syntactik4json" ? Module.TargetFormats.Json:Module.TargetFormats.Xml;
            string modeName = _targetFormat == Module.TargetFormats.Json ? "SyntactikJsonSyntaxMode":"SyntactikSyntaxMode";
            ResourceStreamProvider provider = new ResourceStreamProvider(typeof(SyntactikSyntaxMode).Assembly, typeof(SyntactikSyntaxMode).Assembly.GetManifestResourceNames().First(s => s.Contains(modeName)));
            using (var stream = provider.Open())
            {
                SyntaxMode baseMode = Read(stream);
                this.rules = new List<Rule>(baseMode.Rules);
                this.keywords = new List<Keywords>(baseMode.Keywords);
                this.spans = new List<Span>(baseMode.Spans).ToArray();
                this.matches = baseMode.Matches;
                this.prevMarker = baseMode.PrevMarker;
                this.SemanticRules = new List<SemanticRule>(baseMode.SemanticRules);
                this.keywordTable = baseMode.keywordTable;
                this.keywordTableIgnoreCase = baseMode.keywordTableIgnoreCase;
                this.properties = baseMode.Properties;
            }
            SetDelimiter("");
            doc.TextReplaced += doc_TextReplaced;
        }

        void doc_TextReplaced(object sender, DocumentChangeEventArgs e)
        {
            if (e.ChangeDelta == -1 || (e.ChangeDelta == 1 && e.InsertedText.Text == "\t"))
                SyntaxModeService.StartUpdate(doc, this, e.Offset, e.Offset + e.InsertionLength);
        }

        public override SpanParser CreateSpanParser(DocumentLine line, CloneableStack<Span> spanStack)
        {
            return new SyntactikSpanParser(this, spanStack ?? line.StartSpan.Clone(), _targetFormat, line);
        }
    }
}