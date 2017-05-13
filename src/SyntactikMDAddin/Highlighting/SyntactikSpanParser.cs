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
            if (CurRule != null && CurRule.Name == "open_string" && !(CurSpan is IndentSpan))
            {
                var indent = CurText.TakeWhile(c => c == ' ' || c == '\t').Count();
                FoundSpanBegin(new IndentSpan() {Indent = indent, FirstLine = true, Rule = "open_string", Color = "String"}, i, 0);
                return true;

            }

            return base.ScanSpan(ref i);
        }

        protected override bool ScanSpanEnd(Span cur, ref int i)
        {
            if (CurRule != null && CurRule.Name == "open_string" && CurSpan is IndentSpan)
            {
                var indentSpan = (IndentSpan) CurSpan;
                if (indentSpan.FirstLine && i == StartOffset)
                {
                    indentSpan.FirstLine = false;
                }

                if (!indentSpan.FirstLine)
                {
                    var indent = CurText.TakeWhile(c => c == ' ' || c == '\t').Count();
                    if (indent <= indentSpan.Indent)
                    {
                        FoundSpanEnd(CurSpan, i, 0);
                        return true;
                    }
                }
            }
            return base.ScanSpanEnd(cur, ref i);
        }
    }

    public class IndentSpan : Span
    {
        public int Indent;
        public bool FirstLine;
    }

}
