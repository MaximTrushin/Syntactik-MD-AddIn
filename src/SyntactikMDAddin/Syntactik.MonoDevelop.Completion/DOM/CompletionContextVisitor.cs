using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    class CompletionContextVisitor: SyntactikDepthFirstVisitor
    {
        private Pair _lastPair;

        public Pair LastPair => _lastPair;

        public override void Visit(Syntactik.DOM.Document pair)
        {
            _lastPair = pair;
            base.Visit(pair);
        }

        public override void Visit(Syntactik.DOM.Element pair)
        {
            _lastPair = pair;
            base.Visit(pair);
        }



        public override void Visit(Syntactik.DOM.Attribute pair)
        {
            _lastPair = pair;
            base.Visit(pair);
        }

        public override void Visit(Syntactik.DOM.Alias pair)
        {
            _lastPair = pair;
            base.Visit(pair);
        }
        public override void Visit(Syntactik.DOM.Argument pair)
        {
            _lastPair = pair;
            base.Visit(pair);
        }
        public override void Visit(Syntactik.DOM.Parameter pair)
        {
            _lastPair = pair;
            base.Visit(pair);
        }

        public override void Visit(Syntactik.DOM.AliasDefinition pair)
        {
            _lastPair = pair;
            base.Visit(pair);
        }

        public override void Visit(Syntactik.DOM.NamespaceDefinition pair)
        {
            _lastPair = pair;
            base.Visit(pair);
        }

        public override void Visit(Syntactik.DOM.Scope pair)
        {
            _lastPair = pair;
            base.Visit(pair);
        }
    }
}
