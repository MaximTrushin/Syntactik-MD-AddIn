using MonoDevelop.Ide.CodeCompletion;


namespace Syntactik.MonoDevelop.Completion
{
    public class SyntactikCompletionCategory : CompletionCategory
    {
        public int Order { get; set; }

        public override int CompareTo(CompletionCategory other)
        {
            if (other == null)
                return 0;
            return Order - ((SyntactikCompletionCategory)other).Order;
        }
    }
}
