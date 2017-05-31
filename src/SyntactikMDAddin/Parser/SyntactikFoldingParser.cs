using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;

namespace Syntactik.MonoDevelop.Parser
{
    public class SyntactikFoldingParser: IFoldingParser
    {
        public ParsedDocument Parse(string fileName, string content) 
        {
            var regionStack = new Stack<Tuple<string, DocumentLocation>>();
            var result = new DefaultParsedDocument(fileName);

            return result;
        }
    }
}
