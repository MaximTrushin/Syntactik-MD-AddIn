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
            if (CurSpan == null) return base.ScanSpan(ref i);

            if ((CurRule.Name == "free_open_string" || CurRule.Name == "open_string") && 
                !(CurSpan is IndentSpan))
            {
                if (CurRule.Name == "open_string")
                {
                    var r = new System.Text.RegularExpressions.Regex("[^=:()'\",]*$").Match(CurText, i - StartOffset);
                    if (!r.Success || r.Index != i - StartOffset)
                    {
                        FoundSpanBegin(new Span { Rule = "sl_open_string", Color = "String" }, i, 0);
                        return true;
                    }
                }
                //calculating indent span for multiline strings
                var indent = CurText.TakeWhile(c => c == ' ' || c == '\t').Count();
                FoundSpanBegin(new IndentSpan() {Indent = indent, FirstLine = true, Rule = CurRule.Name, Color = "String"}, i, 0);
                return true;
            }
            return base.ScanSpan(ref i);
        }

        protected override bool ScanSpanEnd(Span cur, ref int i)
        {
            if (CurSpan == null) return base.ScanSpanEnd(cur, ref i);

            if ((CurRule.Name == "free_open_string" || CurRule.Name == "open_string") &&
                CurSpan is IndentSpan)
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
                        FoundSpanEnd(CurSpan, i, 0);
                        return false; //return false so the current symbol will be processed with match rules
                    }
                }
            }
            else if (CurRule.Name == "sl_open_string")
            {
                var r = new System.Text.RegularExpressions.Regex("[^=:()'\",]*").Match(CurText, i - StartOffset);
                if (r.Success)
                {
                    FoundSpanEnd(CurSpan, i, r.Length);
                    FoundSpanEnd(CurSpan, i, r.Length);
                    i += r.Length-1;
                    return true;
                }
                FoundSpanEnd(CurSpan, i, 0);
                FoundSpanEnd(CurSpan, i, 0);
                return false;
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
