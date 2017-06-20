using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Ide.CodeCompletion;


namespace Syntactik.MonoDevelop.Completion
{
    public class SyntactikCategory : CompletionCategory
    {
        public int Order { get; set; }

        public override int CompareTo(CompletionCategory other)
        {
            if (other == null)
                return 0;
            return Order - ((SyntactikCategory)other).Order;
        }
    }
}
