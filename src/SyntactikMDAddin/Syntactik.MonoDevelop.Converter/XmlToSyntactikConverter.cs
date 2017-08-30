using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Schema;

namespace Syntactik.MonoDevelop.Converter
{
    internal class ElementInfo
    {
        public int BlockCounter;
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
                                GetNsAndName(xmlReader.Name, out ns, out name);
                                if (ns != null)
                                {
                                    _sb.Append(ns);
                                    _sb.Append(".");
                                }
                                _sb.Append(name);
                                _value = null;
                                _newLine = false;
                                IncreaseBlockCounter();
                                _elementStack.Push(new ElementInfo {BlockCounter = 0});
                                if (xmlReader.HasAttributes) ProcessAttributes(xmlReader);
                                
                                break;
                            case XmlNodeType.EndElement:
                                if (_value != null)
                                {
                                    WriteValue(_value.ToString());
                                    _value = null;
                                }
                                _currentIndent--;
                                _elementStack.Pop();
                                break;
                            case XmlNodeType.Attribute:
                                StartWithNewLine();

                                GetNsAndName(xmlReader.Name, out ns, out name);
                                if (ns != null)
                                {
                                    _sb.Append(ns);
                                    _sb.Append(".");
                                }
                                _sb.Append(name);
                                _value = null;
                                _newLine = false;
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

        private void ProcessAttributes(XmlTextReader xmlReader)
        {
            _currentIndent++;
            xmlReader.MoveToFirstAttribute();
            do
            {
                StartWithNewLine();
                _sb.Append('@');
                string ns, name;
                GetNsAndName(xmlReader.Name, out ns, out name);
                if (ns != null)
                {
                    _sb.Append(ns);
                    _sb.Append(".");
                }
                _sb.Append(name);
                _newLine = false;
                IncreaseBlockCounter();
                if (!string.IsNullOrEmpty(xmlReader.Value)) WriteValue(xmlReader.Value);
            } while (xmlReader.MoveToNextAttribute());
            _currentIndent--;
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
