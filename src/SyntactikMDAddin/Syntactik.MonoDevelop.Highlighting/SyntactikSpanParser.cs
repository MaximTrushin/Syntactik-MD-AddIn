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

            if ((CurRule.Name.StartsWith("free_open_string") || CurRule.Name.StartsWith("open_string")) && 
                !(CurSpan is IndentSpan))
            {
                char quote = (char) 0;
                var indent = CurText.TakeWhile(c => c == ' ' || c == '\t').Count(); //calculating indent span for multiline strings
                if (CurRule.Name.StartsWith("open_string"))
                {
                    var r = new System.Text.RegularExpressions.Regex(@"\s*('|"")").Match(CurText, i - StartOffset);
                    if (r.Success && r.Index == i - StartOffset)
                    {
                        quote = r.Groups[1].Value[0];
                        var prev = RuleStack.Count > 1 ? RuleStack.ElementAt(1) : null;
                        string ruleName;
                        if (quote == '\'')
                        {
                            ruleName = "sq_string";
                        }
                        else
                        {
                            ruleName = "dq_string";
                        }
                        if (CurRule.Name.EndsWith("_sl")) ruleName += "_sl";
                        FoundSpanBegin(new IndentSpan
                                {
                                    Indent = indent,
                                    FirstLine = true,
                                    Rule = ruleName,
                                    Color = prev == null || !prev.Name.StartsWith("string_high") ? "Xml Text": "String",
                                    Quote = quote }, i, 0
                               );
                        i += r.Length - 1;
                        return false;
                    }
                }
                var prevRule = RuleStack.Count > 1 ? RuleStack.ElementAt(1) : null;
                FoundSpanBegin(new IndentSpan
                {
                    Indent = indent,
                    FirstLine = true,
                    Rule = CurRule.Name,
                    Color = prevRule == null || !prevRule.Name.StartsWith("string_high") ? "Xml Text" : "String",
                    Quote = quote
                }, i, 0);
                return true;
            }
            return base.ScanSpan(ref i);
        }

        protected override bool ScanSpanEnd(Span cur, ref int i)
        {
            if (CurSpan == null) return base.ScanSpanEnd(cur, ref i);

            if (CurRule.Name.StartsWith("string_high") && IsEndOfHighString(CurText[i - StartOffset]))
            {
                FoundSpanEnd(CurSpan, i, 0);
                return CurSpan != null && base.ScanSpanEnd(CurSpan, ref i); //return false so the current symbol will be processed with match rules
            }


            if (CurRule.Name.StartsWith("open_string") //Ending Open string
                && !(CurSpan is IndentSpan) //If it is first line
                && IsEndOfOpenString(CurText[i - StartOffset]) ) //And end of open string found
            {
                FoundSpanEnd(CurSpan, i, 0);
                if (CurSpan != null) FoundSpanEnd(CurSpan, i, 0);
                if (CurRule.Name.StartsWith("string_high")) FoundSpanEnd(CurSpan, i, 0);
                return CurSpan != null && base.ScanSpanEnd(CurSpan, ref i);
            }

            if (CurRule.Name.EndsWith("_sl") && (CurText[i - StartOffset] == '\r' || CurText[i - StartOffset] == '\n'))
            {
                FoundSpanEnd(CurSpan, i, 1);
                FoundSpanEnd(CurSpan, i, 1);
                if (CurRule.Name.StartsWith("string_high")) FoundSpanEnd(CurSpan, i, 1);
                return true;
            }

            if ((CurRule.Name.StartsWith("free_open_string") || CurRule.Name.StartsWith("open_string") || CurRule.Name.StartsWith("sq_string") || CurRule.Name.StartsWith("dq_string")) &&
                CurSpan is IndentSpan)
            {
                var indentSpan = (IndentSpan) CurSpan;
                if (indentSpan.FirstLine && i == StartOffset)
                {
                    indentSpan.FirstLine = false;
                }

                if ((CurRule.Name.StartsWith("sq_string") || CurRule.Name.StartsWith("dq_string")) && indentSpan.Quote == CurText[i - StartOffset])
                {
                    FoundSpanEnd(CurSpan, i, 1);
                    FoundSpanEnd(CurSpan, i, 1);
                    if (CurRule.Name.StartsWith("string_high")) FoundSpanEnd(CurSpan, i, 1);
                    return true;
                }

                if (CurRule.Name.StartsWith("open_string") && indentSpan.FirstLine && indentSpan.Quote == 0)
                {
                    if (IsEndOfOpenString(CurText[i - StartOffset]))
                    {
                        FoundSpanEnd(CurSpan, i, 0);
                        FoundSpanEnd(CurSpan, i, 0);
                        if (CurRule.Name.StartsWith("string_high")) FoundSpanEnd(CurSpan, i, 0);
                        return CurSpan != null && base.ScanSpanEnd(CurSpan, ref i); //return false so the current symbol will be processed with match rules
                    }
                }

                if (!indentSpan.FirstLine)
                {
                    var indent = CurText.TakeWhile(c => c == ' ' || c == '\t').Count();
                    if (indent <= indentSpan.Indent && CurText.Trim().Length > 0)
                    {
                        FoundSpanEnd(CurSpan, i, 0);
                        FoundSpanEnd(CurSpan, i, 0);
                        if (CurRule.Name.StartsWith("string_high")) FoundSpanEnd(CurSpan, i, 0);
                        return CurSpan != null && base.ScanSpanEnd(CurSpan, ref i); //return false so the current symbol will be processed with match rules
                    }
                }
            }
            return base.ScanSpanEnd(CurSpan, ref i);
        }

        private void ExitWsa()
        {
            if (RuleStack.All(rule => rule.Name != "wsa")) return;

            while (RuleStack.Peek().Name != "wsa") RuleStack.Pop();
        }

        public static bool IsEndOfOpenString(char c)
        {
            if (c > 61) return false;
            return c == '=' || c == ':' || c == ',' || c == '\'' || c == '"' || c == ')' || c == '(';
        }

        public static bool IsEndOfHighString(char c)
        {
            if (c > 61) return false;
            return c == ':' || c == ',' || c == '\'' || c == '"' || c == ')' || c == '(';
        }
    }

    public class IndentSpan : Span
    {
        public int Indent;
        public bool FirstLine;
        public char Quote;
    }

}
