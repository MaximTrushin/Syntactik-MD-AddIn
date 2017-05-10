using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.TextEditor.Highlighting;

namespace Syntactik.MonoDevelop.Highlighting
{
    public class SyntactikSpanParser : SyntaxMode.SpanParser
    {
        public SyntactikSpanParser(SyntaxMode mode, CloneableStack<Span> spanStack) : base(mode, spanStack)
        {
        }

        protected override bool ScanSpan(ref int i)
        {
            return base.ScanSpan(ref i);
        }
    }
}
