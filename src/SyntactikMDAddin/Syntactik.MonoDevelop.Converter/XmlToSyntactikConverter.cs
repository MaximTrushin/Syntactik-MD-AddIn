using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;


namespace Syntactik.MonoDevelop.Converter
{
    internal class ElementInfo
    {
        public int BlockCounter;
        public string DefaultNamespace; ////Used to track default ns declarations
        public ListDictionary NsDeclarations; //Used to track ns declarations
    }
    public class XmlToSyntactikConverter
    {
        private readonly string _text;
        private StringBuilder _sb;
        private StringBuilder _value;
        private bool _newLine = true;
        private string _indent;
        private int _currentIndent = -1;
        Stack<ElementInfo> _elementStack;
        private char _indentChar;
        private int _indentMultiplicity;
        private ListDictionary _declaredNamespaces;

        public XmlToSyntactikConverter(string text)
        {
            _text = text;
        }

        public bool Convert(int indent, char indentChar, int indentMultiplicity, bool insertNewLine, out string s4x)
        {
            _indentChar = indentChar;
            _indentMultiplicity = indentMultiplicity;

            _indent = new string(indentChar, indent * indentMultiplicity);
            _sb = new StringBuilder();
            _elementStack = new Stack<ElementInfo>();
            _declaredNamespaces = new ListDictionary();
            if (insertNewLine) _sb.AppendLine("");

            s4x = "";

            //, new XmlReaderSettings()
            //  {
            //      ConformanceLevel = ConformanceLevel.Fragment,
            //      CloseInput = true,
            //      IgnoreComments = true,
            //      IgnoreProcessingInstructions = true,
            //      DtdProcessing = DtdProcessing.Ignore,
            //      ValidationFlags = XmlSchemaValidationFlags.None
            //  }
            using (var xmlReader = new XmlTextReader(new StringReader(_text)))
            {
                xmlReader.Namespaces = false;
                try
                {
                    //var e = xmlReader.ReadElementString();
                    //xmlReader.MoveToContent();
                    while (xmlReader.Read())
                    {
                        string ns;
                        string name;
                        switch (xmlReader.NodeType)
                        {
                            case XmlNodeType.Element:
                                _currentIndent++;
                                StartWithNewLine();
                                _elementStack.Push(new ElementInfo { BlockCounter = 0 });
                                GetNsAndName(xmlReader.Name, out ns, out name);
                                List<Tuple<string, string, string>> attributes = null;
                                if (xmlReader.HasAttributes) attributes = ProcessAttributes(xmlReader);

                                if (ns != null)
                                {
                                    _sb.Append(ResolveNsPrefix(ns));
                                    _sb.Append(".");
                                }
                                _sb.Append(name);
                                _value = null;
                                _newLine = false;
                                
                                
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
                                break;
                            case XmlNodeType.EndElement:
                                if (_value != null)
                                {
                                    WriteValue(_value.ToString());
                                    _value = null;
                                }
                                _currentIndent--;
                                _elementStack.Pop();
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
                catch (Exception)
                {
                    return false;
                }
            }
            s4x = _sb.ToString();
            return true;
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
            var conv = HttpUtility.JavaScriptStringEncode(s, false);

            if (s.Length != conv.Length || s.StartsWith(" ") || s.EndsWith(" "))
            {
                _sb.Append(" == '");
                _sb.Append(s);
                _sb.Append("'");
            }
            else
            {
                _sb.Append(" = ");
                _sb.Append(s);
            }
        }

        private List<Tuple<string, string, string>> ProcessAttributes(XmlTextReader xmlReader)
        {
            xmlReader.MoveToFirstAttribute();
            List<Tuple<string, string, string>> attributes = null;
            do
            {
                string ns, name;
                GetNsAndName(xmlReader.Name, out ns, out name);
                if (ns != "xmlns" && !(string.IsNullOrEmpty(ns) && name == "xmlns"))
                {
                    if (attributes == null) attributes = new List<Tuple<string, string, string>>();
                    attributes.Add(new Tuple<string, string, string>(ns, name, xmlReader.Value));
                }
                else
                {
                    ProcessNsAttribute(ns, name, xmlReader.Value);
                }
            } while (xmlReader.MoveToNextAttribute());
            return attributes;
        }

        private void ProcessNsAttribute(string ns, string name, string value)
        {
            if (string.IsNullOrEmpty(ns) && name == "xmlns")
            {
                _elementStack.Peek().DefaultNamespace = value;
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

        //public static string cleanForJSON(string s)
        //{
        //    if (s == null || s.Length == 0)
        //    {
        //        return "";
        //    }

        //    char c = '\0';
        //    int i;
        //    int len = s.Length;
        //    StringBuilder sb = new StringBuilder(len + 4);
        //    String t;

        //    for (i = 0; i < len; i += 1)
        //    {
        //        c = s[i];
        //        switch (c)
        //        {
        //            case '\\':
        //            case '"':
        //                sb.Append('\\');
        //                sb.Append(c);
        //                break;
        //            case '/':
        //                sb.Append('\\');
        //                sb.Append(c);
        //                break;
        //            case '\b':
        //                sb.Append("\\b");
        //                break;
        //            case '\t':
        //                sb.Append("\\t");
        //                break;
        //            case '\n':
        //                sb.Append("\\n");
        //                break;
        //            case '\f':
        //                sb.Append("\\f");
        //                break;
        //            case '\r':
        //                sb.Append("\\r");
        //                break;
        //            default:
        //                if (c < ' ')
        //                {
        //                    t = "000" + String.Format("X", c);
        //                    sb.Append("\\u" + t.Substring(t.Length - 4));
        //                }
        //                else
        //                {
        //                    sb.Append(c);
        //                }
        //                break;
        //        }
        //    }
        //    return sb.ToString();
        //}
    }
}
