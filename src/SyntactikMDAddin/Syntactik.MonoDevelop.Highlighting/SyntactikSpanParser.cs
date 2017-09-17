using System.Linq;
using Mono.TextEditor.Highlighting;
using Syntactik.DOM.Mapped;

namespace Syntactik.MonoDevelop.Highlighting
{
    public class SyntactikSpanParser : SyntaxMode.SpanParser
    {
        private readonly Module.TargetFormats _format;

        public SyntactikSpanParser(SyntaxMode mode, CloneableStack<Span> spanStack, Module.TargetFormats format) : base(mode, spanStack)
        {
            _format = format;
        }

        protected override bool ScanSpan(ref int i)
        {
            if (CurSpan == null) return base.ScanSpan(ref i);

            if ((CurRule.Name.StartsWith("free_open_string") || CurRule.Name.StartsWith("open_string")) && 
                !(CurSpan is IndentSpan))
            {
                char quote = (char) 0;
                var indent = CurText.TakeWhile(c => c == ' ' || c == '\t').Count(); //calculating indent span for multiline strings
                var textStyle = "Xml Text";
                if (CurRule.Name.StartsWith("o")) //"open_string"
                {
                    var r = new System.Text.RegularExpressions.Regex(@"\s*('|"")").Match(CurText, i - StartOffset);
                    if (r.Success && r.Index == i - StartOffset) //quoted string processing
                    {
                        quote = r.Groups[1].Value[0];
                        var prev = RuleStack.Count > 1 ? RuleStack.ElementAt(1) : null;
                        var ruleName = quote == '\'' ? "sq_string" : "dq_string";
                        if (CurRule.Name.EndsWith("_sl")) ruleName += "_sl";
                        FoundSpanBegin(new IndentSpan
                                {
                                    Indent = indent,
                                    FirstLine = true,
                                    Rule = ruleName,
                                    Color = prev == null || !prev.Name.StartsWith("string_high") ? textStyle: "String",
                                    Quote = quote }, i, 0
                               );
                        i += r.Length - 1;
                        return false;
                    }
                    if (_format == Module.TargetFormats.Json) //processing json literals
                    {
                        r = new System.Text.RegularExpressions.Regex(
                                @"\s*(?:false|true|null|-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?)\s*(?=[=:()'"",]|$)").Match(
                                CurText, i - StartOffset);
                        if (r.Success && r.Index == i - StartOffset)
                        {
                            textStyle = "Keyword(Constants)";
                        }
                    }
                }
                
                if (CurRule.Name.StartsWith("f") /*"free_open_string"*/ && _format == Module.TargetFormats.Json) //processing json literals
                {
                    var r = new System.Text.RegularExpressions.Regex(@"\s*(?:false|true|null|-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?)\s*$").Match(CurText, i - StartOffset);
                    if (r.Success && r.Index == i - StartOffset)
                    {
                        textStyle = "Keyword(Constants)";
                    }
                }

                var prevRule = RuleStack.Count > 1 ? RuleStack.ElementAt(1) : null;
                FoundSpanBegin(new IndentSpan
                {
                    Indent = indent,
                    FirstLine = true,
                    Rule = CurRule.Name,
                    Color = prevRule == null || !prevRule.Name.StartsWith("string_high") ? textStyle : "String",
                    Quote = quote
                }, i, 0);
                return false;
            }
            return base.ScanSpan(ref i);
        }

        protected override bool ScanSpanEnd(Span cur, ref int i)
        {
            if (CurSpan == null) return base.ScanSpanEnd(cur, ref i);

            if (CurRule.Name.StartsWith("string_high") 
                && IsEndOfHighString(CurText[i - StartOffset]))
            {
                FoundSpanEnd(CurSpan, i, 0);
                return CurSpan != null && base.ScanSpanEnd(CurSpan, ref i); //return false so the current symbol will be processed with match rules
            }

            if (CurRule.Name.StartsWith("open_string") //Ending Open string
                && IsEndOfOpenString(CurText[i - StartOffset]) ) //And end of open string found
            {
                var c = (CurText[i - StartOffset]);
                var indentSpan = CurSpan as IndentSpan;
                if (c != '\'' && c != '"' || (indentSpan != null && indentSpan.FirstLine)) //quotes end open string on first line only if it's already started 
                {
                    FoundSpanEnd(CurSpan, i, 0);
                    if (CurSpan != null) FoundSpanEnd(CurSpan, i, 0);
                    if (CurRule.Name.StartsWith("string_high")) FoundSpanEnd(CurSpan, i, 0);
                    return CurSpan != null && base.ScanSpanEnd(CurSpan, ref i);
                }
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
                    indentSpan.FirstLine = false; //This is redundant because Span.Clone is not virtual 
                                                   //so FirstLine will have default value for the second line.
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
                    if (CurText.Trim().Length > 0)
                    {
                        if (indent <= indentSpan.Indent)
                        {
                            FoundSpanEnd(CurSpan, i, 0);
                            FoundSpanEnd(CurSpan, i, 0);
                            if (CurRule.Name.StartsWith("string_high")) FoundSpanEnd(CurSpan, i, 0);
                            return CurSpan != null && base.ScanSpanEnd(CurSpan, ref i);
                            //return false so the current symbol will be processed with match rules
                        }

                        if (CurSpan.Color == "Keyword(Constants)")//stop json literals highlightin on the second line
                        {
                            CurSpan.Color = "Xml Text";
                        }
                    }
                }
            }
            return base.ScanSpanEnd(CurSpan, ref i);
        }

        public static bool IsEndOfOpenString(char c)
        {
            if (c > 61) return false;
            return c == '=' || c == ':' || c == ',' || c == '\'' || c == '"' || c == ')' || c == '(';
        }

        public static bool IsEndOfHighString(char c)
        {
            if (c > 61) return false;
            return c == ':' || c == ',' || c == '\'' || c == '"' || c == ')' || c == '(' || c == '\r' || c == '\n';
        }
    }

    public class IndentSpan : Span
    {
        public int Indent;
        public bool FirstLine;
        public char Quote;
    }

}
