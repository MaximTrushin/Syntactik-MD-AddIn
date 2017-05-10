using Mono.TextEditor;
using Mono.TextEditor.Highlighting;

namespace Syntactik.MonoDevelop.Highlighting
{
    public class SyntactikSyntaxMode : SyntaxMode
    {
        public SyntactikSyntaxMode(TextDocument doc) : base(doc)
        {
        }

        public override SpanParser CreateSpanParser(DocumentLine line, CloneableStack<Span> spanStack)
        {
            return new SyntactikSpanParser(this, spanStack ?? line.StartSpan.Clone());
        }
    }
}