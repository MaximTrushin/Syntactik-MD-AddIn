using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Syntactik.MonoDevelop.Converter
{
    internal class ElementInfo
    {
        public int BlockCounter;
        public string DefaultNamespace; // Used to track default ns declarations
        public ListDictionary NsDeclarations; // Used to track ns declarations
    }
    public class XmlToSyntactikConverter
    {
        private readonly bool _validate;
        private readonly bool _withNamespaces;
        private readonly string _text;
        private StringBuilder _sb;
        private StringBuilder _value;
        private bool _newLine = true;
        private string _indent;
        private int _currentIndent = -1;
        private Stack<ElementInfo> _elementStack;
        private char _indentChar;
        private int _indentMultiplicity;
        private ListDictionary _declaredNamespaces;

        /// <summary>
        /// Converts xml to syntactik format
        /// </summary>
        /// <param name="text">xml to convert.</param>
        /// <param name="validate">If true will validate as the whole xml document, otherwise can accept any xml fragment.</param>
        /// <param name="withNamespaces">If true then output will include namespace declarations.</param>
        public XmlToSyntactikConverter(string text, bool validate = false, bool withNamespaces = false)
        {
            _validate = validate;
            _withNamespaces = withNamespaces;
            _text = !validate ? Regex.Replace(text, @"<\?[^?]*(?:\?[^>]+)*\?+>(?:\r|\n)*", "") : text;
        }

        private string DefaultNamespace
        {
            get
            {
                if (_elementStack == null || _elementStack.Count == 0) return null;
                return _elementStack.Peek().DefaultNamespace;
            }
        }

        public bool Convert(int indent, char indentChar, int indentMultiplicity, bool insertNewLine, ListDictionary declaredNamespaces, out string s4x)
        {
            _indentChar = indentChar;
            _indentMultiplicity = indentMultiplicity;

            _indent = new string(indentChar, indent * indentMultiplicity);
            _sb = new StringBuilder();
            _elementStack = new Stack<ElementInfo>();
            _declaredNamespaces = declaredNamespaces;
            if (insertNewLine)
            {
                _sb.AppendLine("");
                _sb.Append(_indent);
            }
            using(var stringReader = new StringReader(_text))
            using (var xmlReader = _validate? 
                        new XmlTextReader(stringReader):
                        //XmlReader.Create(stringReader,new XmlReaderSettings() {ConformanceLevel = ConformanceLevel.Document}) : 
                        new XmlTextReader(new FakeRootStreamReader(stringReader)))
            {
                if (!_validate)
                {
                    ((XmlTextReader)xmlReader).Namespaces = false;
                    ((XmlTextReader)xmlReader).DtdProcessing = DtdProcessing.Ignore;
                    xmlReader.Read();
                }
                try
                {
                    while (xmlReader.Read())
                    {
                        switch (xmlReader.NodeType)
                        {
                            case XmlNodeType.Element:
                                _currentIndent++;
                                StartWithNewLine();
                                _elementStack.Push(new ElementInfo { BlockCounter = 0, DefaultNamespace = DefaultNamespace});
                                string ns;
                                string name;
                                GetNsAndName(xmlReader.Name, out ns, out name);
                                List<Tuple<string, string, string>> attributes = null;
                                string defaultNsPrefix = null;
                                if (xmlReader.HasAttributes) attributes = ProcessAttributes(xmlReader, out defaultNsPrefix);
                                if (ns != null)
                                {
                                    var resolvedNs = ResolveNsPrefix(ns);
                                    if (resolvedNs == defaultNsPrefix)
                                    {
                                        _sb.Append("#");
                                    }
                                    else
                                    {
                                        if (defaultNsPrefix != null)
                                        {
                                            _sb.Append("#");
                                            _sb.Append(defaultNsPrefix);
                                            _sb.Append(":");
                                        }
                                    }
                                    _sb.Append(resolvedNs);
                                    _sb.Append(".");
                                }
                                else if (defaultNsPrefix != null)
                                {
                                    _sb.Append("#");
                                    _sb.Append(defaultNsPrefix);
                                    _sb.Append(".");
                                }
                                _sb.Append(name);
                                _value = null;
                                _newLine = false;
                                ProcessAttributes(attributes);
                                break;
                            case XmlNodeType.EndElement:
                                if (_value != null)
                                {
                                    WriteValue(_value.ToString());
                                    _value = null;
                                }
                                _currentIndent--;
                                if (_elementStack.Count > 0) _elementStack.Pop();
                                IncreaseBlockCounter();
                                break;
                            case XmlNodeType.Text:
                                if (_value == null) _value = new StringBuilder();
                                _value.Append(xmlReader.Value);
                                break;
                            case XmlNodeType.CDATA:
                                if (_value == null) _value = new StringBuilder();
                                _value.Append(xmlReader.Value);
                                break;
                            case XmlNodeType.ProcessingInstruction:

                                break;
                            case XmlNodeType.Comment:

                                break;
                            case XmlNodeType.XmlDeclaration:

                                break;
                            case XmlNodeType.Document:
                                break;
                            case XmlNodeType.DocumentType:

                                break;
                            case XmlNodeType.EntityReference:

                                break;
                        }
                    }
                }
                catch
                {
                    s4x = _withNamespaces?GetNamespaceDeclarations(_declaredNamespaces) + _sb: _sb.ToString();
                    return false;
                }
            }
            s4x = _withNamespaces ? GetNamespaceDeclarations(_declaredNamespaces) + _sb : _sb.ToString();
            return true;
        }

        private static string GetNamespaceDeclarations(ListDictionary declaredNamespaces)
        {
            var sb = new StringBuilder();
            foreach (DictionaryEntry ns in declaredNamespaces)
            {
                sb.Append("!#");
                sb.Append(ns.Key);
                sb.Append(" = ");
                sb.AppendLine(ns.Value.ToString());
            }
            return sb.ToString();
        }

        private void ProcessAttributes(List<Tuple<string, string, string>> attributes)
        {
            if (attributes != null)
            {
                _currentIndent++;
                foreach (var tuple in attributes)
                {
                    StartWithNewLine();
                    _sb.Append('@');
                    if (tuple.Item1 != null) //namespace prefix
                    {
                        _sb.Append(tuple.Item1);
                        _sb.Append(".");
                    }
                    _sb.Append(tuple.Item2); // name
                    _newLine = false;
                    IncreaseBlockCounter();
                    if (!string.IsNullOrEmpty(tuple.Item3)) WriteValue(tuple.Item3);
                }
                _currentIndent--;
            }
        }

        private string ResolveNsPrefix(string ns)
        {
            var @namespace = GetNamespace(ns);

            if (@namespace == null) return ns;
            foreach (var declaredNamespace in _declaredNamespaces)
            {
                var entry = (DictionaryEntry)declaredNamespace;
                if (entry.Value.ToString() == @namespace) return (string)entry.Key;
            }
            return ns;
        }

        private string LookupOrCreatePrefix(string @namespace)
        {
            string ns = null;
            foreach (var declaredNamespace in _declaredNamespaces)
            {
                var entry = (DictionaryEntry)declaredNamespace;
                if (entry.Value.ToString() == @namespace)
                {
                    ns = (string)entry.Key;
                }
            }
            if (ns != null) return ns;
            var i = 1;
            while (true)
            {
                var newNs = "ns" + i;
                if (GetDeclaredNamespace(newNs) == null)
                {
                    _declaredNamespaces[newNs] = @namespace;
                    return newNs;
                }
                i++;
            }
        }

        private string GetDeclaredNamespace(string prefix)
        {
            foreach (var declaredNamespace in _declaredNamespaces)
            {
                var entry = (DictionaryEntry)declaredNamespace;
                if (entry.Key.ToString() == prefix)
                {
                    return (string)entry.Value;
                }
            }
            return null;
        }

        private string GetNamespace(string ns)
        {
            foreach (var elementInfo in _elementStack)
            {
                if (elementInfo.NsDeclarations == null) continue;
                foreach (var item in elementInfo.NsDeclarations)
                {
                    var entry = (DictionaryEntry)item;
                    if (entry.Key.ToString() == ns) return (string) entry.Value;
                }
            }
            return null;
        }

        private void WriteValue(string s)
        {
            bool escapeSymbolsFound;
            var conv = EncodeValue(s, out escapeSymbolsFound);

            if (escapeSymbolsFound || s.StartsWith(" ") || s.EndsWith(" "))
            {
                _sb.Append(" == '");
                _sb.Append(conv);
                _sb.Append("'");
            }
            else
            {
                _sb.Append(" = ");
                _sb.Append(s);
            }
        }

        private List<Tuple<string, string, string>> ProcessAttributes(XmlReader xmlReader, out string defaultNsPrefix)
        {
            defaultNsPrefix = null;
            xmlReader.MoveToFirstAttribute();
            List<Tuple<string, string, string>> attributes = new List<Tuple<string, string, string>>();
            do
            {
                string ns, name;
                GetNsAndName(xmlReader.Name, out ns, out name);
                if (ns == "xsi" || ns == "xml")
                {
                    //Process xsi and xml attributes
                    var value = xmlReader.Value;
                    if (ns == "xsi" && name == "type")
                    {
                        var s = value.Split(new[] {':',}, StringSplitOptions.RemoveEmptyEntries);
                        if (s.Length == 2)
                        {
                            var prefix = ResolveNsPrefix(s[0]);
                            value = $"{prefix}:{s[1]}";
                        }
                    }

                    if (attributes.Count > 0)
                        attributes.Insert(0, new Tuple<string, string, string>(ns, name, value));
                    else
                        attributes.Add(new Tuple<string, string, string>(ns, name, value));
                }
                else if (ns != "xmlns" && !(string.IsNullOrEmpty(ns) && name == "xmlns"))
                {
                    //process regular attributes
                    if (!string.IsNullOrEmpty(ns))
                    {
                        ns = ResolveNsPrefix(ns);
                    }
                    attributes.Add(new Tuple<string, string, string>(ns, name, xmlReader.Value));
                }
                else
                {
                    //Process namespace declaration
                    ProcessNsAttribute(ns, name, xmlReader.Value, out defaultNsPrefix);
                }
            } while (xmlReader.MoveToNextAttribute());
            return attributes;
        }

        private void ProcessNsAttribute(string ns, string name, string value, out string defaultNsPrefix)
        {
            defaultNsPrefix = null;
            if (string.IsNullOrEmpty(ns) && name == "xmlns")
            {
                _elementStack.Peek().DefaultNamespace = value;
                defaultNsPrefix = LookupOrCreatePrefix(value);
                return;
            }
            if (ns == "xmlns" && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
            {
                //if (name == "xsi") return; //ignoring xsi namespace declaration
                var elementInfo = _elementStack.Peek();
                if (elementInfo.NsDeclarations == null) elementInfo.NsDeclarations = new ListDictionary();
                elementInfo.NsDeclarations[name] = value;
                foreach (var declaredNamespace in _declaredNamespaces)
                {
                    var entry = (DictionaryEntry)declaredNamespace;
                    if (entry.Value.ToString() == value) return;
                }
                _declaredNamespaces[name] = value;
            }
        }

        private void GetNsAndName(string localName, out string ns, out string name)
        {
            var names = localName.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            name = localName;
            ns = null;
            if (names.Length == 2)
            {
                ns = names[0];
                name = names[1];
            }
        }

        private void IncreaseBlockCounter()
        {
            if (_elementStack.Count == 0) return;
            var elementInfo = _elementStack.Peek();
            elementInfo.BlockCounter++;
        }

        private void StartWithNewLine()
        {
            if (_newLine) return;
            if (FirstNodeInBlock) _sb.Append(":");
            _sb.AppendLine();
            _sb.Append(_indent);
            _sb.Append(_indentChar, _currentIndent * _indentMultiplicity);
            _newLine = true;
        }

        public bool FirstNodeInBlock => _elementStack.Count > 0 && _elementStack.Peek().BlockCounter == 0;

        public ListDictionary DeclaredNamespaces
        {
            get { return _declaredNamespaces; }
        }

        public static string EncodeValue(string s, out bool escapeSymbolsFound)
        {
            escapeSymbolsFound = false;
            if (string.IsNullOrEmpty(s)) return "";
            int i;
            var len = s.Length;
            var sb = new StringBuilder(len + 4);

            for (i = 0; i < len; i += 1)
            {
                var c = s[i];
                switch (c)
                {
                    case '\'':
                    case '\\':
                    case '/':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '\b':
                        sb.Append("\\b");
                        escapeSymbolsFound = true;
                        break;
                    case '\t':
                        sb.Append("\\t");
                        escapeSymbolsFound = true;
                        break;
                    case '\n':
                        sb.Append("\\n");
                        escapeSymbolsFound = true;
                        break;
                    case '\f':
                        sb.Append("\\f");
                        escapeSymbolsFound = true;
                        break;
                    case '\r':
                        sb.Append("\\r");
                        escapeSymbolsFound = true;
                        break;
                    default:
                        if (c < ' ')
                        {
                            var t = "000" + $"{System.Convert.ToInt32(c):X}";
                            sb.Append("\\u" + t.Substring(t.Length - 4));
                            escapeSymbolsFound = true;
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
