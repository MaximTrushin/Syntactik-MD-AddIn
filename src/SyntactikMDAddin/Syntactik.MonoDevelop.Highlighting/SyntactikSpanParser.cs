using System.Linq;
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using Syntactik.DOM.Mapped;

namespace Syntactik.MonoDevelop.Highlighting
{
    class SyntactikSpanParser : SyntaxMode.SpanParser
    {
        private readonly Module.TargetFormats _format;
        private readonly DocumentLine _line;
        private bool _inline;

        public SyntactikSpanParser(SyntaxMode mode, CloneableStack<Span> spanStack, Module.TargetFormats format, DocumentLine line) : base(mode, spanStack)
        {
            _format = format;
            _line = line;
        }

        /// <summary>
        /// Rules:
        /// "h_" - highlighted string - value of namespace definition or attribute.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        protected override bool ScanSpan(ref int i)
        {
            if (i == StartOffset) _inline = false;

            if (CurSpan == null) return base.ScanSpan(ref i);

            if ((CurRule.Name.StartsWith("f") || CurRule.Name.StartsWith("o")) && 
                !(CurSpan is IndentSpan))
            {
                char quote = (char) 0;
                int indent;
                if (_line != null)
                {
                    indent = _line.GetIndentation(mode.Document).Length;
                }
                else
                {
                    var l = mode.Document.GetLineByOffset(i);
                    indent = (l?.GetIndentation(mode.Document).Length)??0;
                    var indent2 = CurText.TakeWhile(c => c == ' ' || c == '\t').Count();

                    if (indent2 > indent)
                    {
                        indent = indent2;
                    }
                }
                
                var textStyle = "Xml Text";
                var r = new System.Text.RegularExpressions.Regex(@"\s*('(?!'')|""(?!""""'))").Match(CurText, i - StartOffset);//starts with quote but not a comment?
                if (r.Success && r.Index == i - StartOffset) //quoted string processing
                {
                    quote = r.Groups[1].Value[0];
                    var prev = RuleStack.Count > 1 ? RuleStack.ElementAt(1) : null;
                    var ruleName = quote == '\'' ? "sq_string" : "dq_string";
                    if (CurRule.Name.EndsWith("_sl") || _inline) ruleName += "_sl";
                    FoundSpanBegin(new IndentSpan
                        {
                            Indent = indent,
                            FirstLine = true,
                            Rule = ruleName,
                            Color = prev == null || !prev.Name.StartsWith("h_") ? textStyle : "String",
                            Quote = quote
                        }, i, 0
                    );
                    i += r.Length - 1;
                    return false;
                }

                r = new System.Text.RegularExpressions.Regex(@"\s*('''|"""""")").Match(CurText, i - StartOffset);//comment?
                if (r.Success && r.Index == i - StartOffset)
                {
                    return false;
                }

                if (_format == Module.TargetFormats.Json) //processing json literals
                {
                    //System.Text.RegularExpressions.Match r;
                    if (CurRule.Name.StartsWith("f") /*"free_open_string"*/)
                    {
                        r = new System.Text.RegularExpressions.Regex(
                                @"\s*(?:false|true|null|-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?)\s*$").Match(
                                CurText, i - StartOffset);
                    }
                    else
                    {
                        r = new System.Text.RegularExpressions.Regex(
                                @"\s*(?:false|true|null|-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?)\s*(?=[=:()'"",]|$)").Match(
                                CurText, i - StartOffset);
                    }

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
                    Rule = CurRule.Name + (_inline && !CurRule.Name.EndsWith("_sl") ? "_sl":""),
                    Color = prevRule == null || !prevRule.Name.StartsWith("h_") ? textStyle : "String",
                    Quote = quote
                }, i, 0);
                return false;
            }
            return base.ScanSpan(ref i);
        }

        protected override bool ScanSpanEnd(Span cur, ref int i)
        {
            if (i == StartOffset)
                _inline = false;

            if (CurSpan == null) return base.ScanSpanEnd(cur, ref i);

            if (CurRule.Name.StartsWith("pu")) //punctuation
            {
                FoundSpanEnd(CurSpan, i, 0);
                _inline = true;
                return false;
            }

            if (CurRule.Name.StartsWith("h_") 
                && IsEndOfHighString(CurText[i - StartOffset]))
            {
                FoundSpanEnd(CurSpan, i, 0);
                return CurSpan != null && base.ScanSpanEnd(CurSpan, ref i); //return false so the current symbol will be processed with match rules
            }

            var indentSpan = CurSpan as IndentSpan;

            if (indentSpan == null && (CurRule.Name.StartsWith("o") || CurRule.Name.StartsWith("f")))
            {
                var r = new System.Text.RegularExpressions.Regex(@"\s*('''|"""""")").Match(CurText, i - StartOffset);//comment?
                if (r.Success && r.Index == i - StartOffset)
                {
                    FoundSpanEnd(CurSpan, i, 0);
                    if (CurRule.Name.StartsWith("h_")) FoundSpanEnd(CurSpan, i, 0);
                    return CurSpan != null && base.ScanSpanEnd(CurSpan, ref i);
                }
            }


            if (CurRule.Name.StartsWith("o") //Ending Open string
                && (indentSpan == null || indentSpan.FirstLine) // First line only
                && IsEndOfOpenString(CurText[i - StartOffset]) ) //And end of open string found
            {
                var c = (CurText[i - StartOffset]);
                if (c != '\'' && c != '"' || (indentSpan != null && indentSpan.FirstLine)) //quotes end open string on first line only if it's already started 
                {
                    FoundSpanEnd(CurSpan, i, 0);
                    if (CurSpan != null) FoundSpanEnd(CurSpan, i, 0);
                    if (CurRule.Name.StartsWith("h_")) FoundSpanEnd(CurSpan, i, 0);
                    return CurSpan != null && base.ScanSpanEnd(CurSpan, ref i);
                }
            }

            if (CurRule.Name.EndsWith("_sl") && (CurText[i - StartOffset] == '\r' || CurText[i - StartOffset] == '\n'))
            {
                FoundSpanEnd(CurSpan, i, 1);
                FoundSpanEnd(CurSpan, i, 1);
                if (CurRule.Name.StartsWith("h_")) FoundSpanEnd(CurSpan, i, 1);
                return true;
            }
           
            if (indentSpan != null && 
                    (
                        CurRule.Name.StartsWith("f") || 
                        CurRule.Name.StartsWith("o") || 
                        CurRule.Name.StartsWith("s") || 
                        CurRule.Name.StartsWith("d")
                    ) 
                )
            {
                
                if (indentSpan.FirstLine && i == StartOffset)
                {
                    indentSpan.FirstLine = false; //Span.Clone is not virtual 
                                                   //so FirstLine will have default value for the second line.
                }

                if ((CurRule.Name.StartsWith("s") || CurRule.Name.StartsWith("d")) && indentSpan.Quote == CurText[i - StartOffset])
                {
                    FoundSpanEnd(CurSpan, i, 1);
                    FoundSpanEnd(CurSpan, i, 1);
                    if (CurRule.Name.StartsWith("h_")) FoundSpanEnd(CurSpan, i, 1);
                    return true;
                }

                if (CurRule.Name.StartsWith("o") && indentSpan.FirstLine && indentSpan.Quote == 0)
                {
                    if (IsEndOfOpenString(CurText[i - StartOffset]))
                    {
                        FoundSpanEnd(CurSpan, i, 0);
                        FoundSpanEnd(CurSpan, i, 0);
                        if (CurRule.Name.StartsWith("h_")) FoundSpanEnd(CurSpan, i, 0);
                        return CurSpan != null && base.ScanSpanEnd(CurSpan, ref i);
                    }
                }

                if (!indentSpan.FirstLine)
                {
                    int indent;
                    if (_line != null)
                    {
                        indent = _line.GetIndentation(mode.Document).Length;
                    }
                    else
                    {
                        var l = mode.Document.GetLineByOffset(i);
                        indent = l?.GetIndentation(mode.Document).Length??0;
                        var indent2 = CurText.TakeWhile(c => c == ' ' || c == '\t').Count();
                        if (indent2 > indent)
                        {
                            indent = indent2;
                        }
                    }

                    if (CurText.Trim().Length > 0)
                    {
                        if (indent <= indentSpan.Indent)
                        {
                            FoundSpanEnd(CurSpan, i, 0);
                            FoundSpanEnd(CurSpan, i, 0);
                            if (CurRule.Name.StartsWith("h_")) FoundSpanEnd(CurSpan, i, 0);
                            return CurSpan != null && base.ScanSpanEnd(CurSpan, ref i);
                            //return false so the current symbol will be processed with match rules
                        }

                        if (CurSpan.Color == "Keyword(Constants)")//stop json literals highlighting on the second line
                        {
                            CurSpan.Color = "Xml Text";
                        }
                    }
                }
            }

            if (indentSpan == null && //rules sq_string or dq_string started from syntax mode but not by span parser
                (
                    CurRule.Name.StartsWith("s") && CurText[i - StartOffset] == '\'' || 
                    CurRule.Name.StartsWith("d") && CurText[i - StartOffset] == '"'
                )
            )
            {
                if (FollowedByDelimiter(CurText, i - StartOffset))
                {
                    CurSpan.Color = "Keyword(Type)";
                }
                FoundSpanEnd(CurSpan, i, 1);
                return true;

            }
            return base.ScanSpanEnd(CurSpan, ref i);
        }

        private static bool FollowedByDelimiter(string curText, int i)
        {
            var length = curText.Length;
            while (++i < length)
            {
                if (curText[i] == ':' || curText[i] == '=') return true;
                if (curText[i] != ' ' && curText[i] == '\t') break;
            }
            return false;
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

    class IndentSpan : Span
    {
        public int Indent;
        public bool FirstLine;
        public char Quote;
    }

}
