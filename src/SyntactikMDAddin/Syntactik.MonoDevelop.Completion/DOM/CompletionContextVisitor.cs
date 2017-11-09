using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    class CompletionContextVisitor: SyntactikDepthFirstVisitor
    {
        private Pair _lastPair;

        public Pair LastPair => _lastPair;

        public override void OnDocument(Syntactik.DOM.Document pair)
        {
            _lastPair = pair;
            base.OnDocument(pair);
        }

        public override void OnElement(Syntactik.DOM.Element pair)
        {
            _lastPair = pair;
            base.OnElement(pair);
        }



        public override void OnAttribute(Syntactik.DOM.Attribute pair)
        {
            _lastPair = pair;
            base.OnAttribute(pair);
        }

        public override void OnAlias(Syntactik.DOM.Alias pair)
        {
            _lastPair = pair;
            base.OnAlias(pair);
        }
        public override void OnArgument(Syntactik.DOM.Argument pair)
        {
            _lastPair = pair;
            base.OnArgument(pair);
        }
        public override void OnParameter(Syntactik.DOM.Parameter pair)
        {
            _lastPair = pair;
            base.OnParameter(pair);
        }

        public override void OnAliasDefinition(Syntactik.DOM.AliasDefinition pair)
        {
            _lastPair = pair;
            base.OnAliasDefinition(pair);
        }

        public override void OnNamespaceDefinition(Syntactik.DOM.NamespaceDefinition pair)
        {
            _lastPair = pair;
            base.OnNamespaceDefinition(pair);
        }

        public override void OnScope(Syntactik.DOM.Scope pair)
        {
            _lastPair = pair;
            base.OnScope(pair);
        }
    }
}
