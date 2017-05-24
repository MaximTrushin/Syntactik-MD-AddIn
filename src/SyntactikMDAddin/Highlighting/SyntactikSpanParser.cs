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
                char quote = (char) 0;
                var indent = CurText.TakeWhile(c => c == ' ' || c == '\t').Count(); //calculating indent span for multiline strings
                if (CurRule.Name == "open_string")
                {
                    var r = new System.Text.RegularExpressions.Regex(@"\s*('|"")").Match(CurText, i - StartOffset);
                    if (r.Success && r.Index == i - StartOffset)
                    {
                        quote = r.Groups[1].Value[0];
                        FoundSpanBegin(new IndentSpan
                                {
                                    Indent = indent,
                                    FirstLine = true,
                                    Rule = quote == '\''? "sq_string": CurRule.Name,
                                    Color = "Xml Text",
                                    Quote = quote }, i, 0
                               );
                        i += r.Length - 1;
                        return false;
                    }
                }
                FoundSpanBegin(new IndentSpan {Indent = indent, FirstLine = true, Rule = CurRule.Name, Color = "Xml Text", Quote = quote}, i, 0);
                return true;
            }
            return base.ScanSpan(ref i);
        }

        protected override bool ScanSpanEnd(Span cur, ref int i)
        {
            if (CurSpan == null) return base.ScanSpanEnd(cur, ref i);

            if ((CurRule.Name == "free_open_string" || CurRule.Name == "open_string" || CurRule.Name == "sq_string") &&
                CurSpan is IndentSpan)
            {
                var indentSpan = (IndentSpan) CurSpan;
                if (indentSpan.FirstLine && i == StartOffset)
                {
                    indentSpan.FirstLine = false;
                }

                if ((CurRule.Name == "open_string" || CurRule.Name == "sq_string") && indentSpan.Quote == CurText[i - StartOffset])
                {
                    FoundSpanEnd(CurSpan, i, 1);
                    FoundSpanEnd(CurSpan, i, 1);
                    return true;
                }

                if (CurRule.Name == "open_string" && indentSpan.FirstLine && indentSpan.Quote == 0)
                {
                    if (IsEndOfOpenString(CurText[i - StartOffset]))
                    {
                        FoundSpanEnd(CurSpan, i, 0);
                        FoundSpanEnd(CurSpan, i, 0);
                        return false; //return false so the current symbol will be processed with match rules
                    }
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
            return base.ScanSpanEnd(cur, ref i);
        }

        public static bool IsEndOfOpenString(char c)
        {
            if (c > 61) return false;
            return c == '=' || c == ':' || c == ',' || c == '\'' || c == '"' || c == ')' || c == '(';
        }
    }

    public class IndentSpan : Span
    {
        public int Indent;
        public bool FirstLine;
        public char Quote;
    }

}
