using Syntactik.DOM;
using Syntactik.MonoDevelop.Completion.DOM;
using Alias = Syntactik.DOM.Alias;
using AliasDefinition = Syntactik.DOM.AliasDefinition;
using Argument = Syntactik.DOM.Argument;
using Document = Syntactik.DOM.Document;
using Element = Syntactik.DOM.Element;
using Parameter = Syntactik.DOM.Parameter;
using Scope = Syntactik.DOM.Scope;


namespace Syntactik.MonoDevelop.Completion
{

    internal class CompletionVisitor : SyntactikDepthFirstVisitor
    {
        protected override void OnPair(Pair pair)
        {
            var cn = pair as ICompletionNode;
            cn?.StoreStringValues();
            base.OnPair(pair);
        }

        public override void Visit(Alias alias)
        {
            base.Visit(alias);
            Visit(alias.PairValue);
        }

        public override void Visit(AliasDefinition aliasDefinition)
        {
            base.Visit(aliasDefinition);
            Visit(aliasDefinition.PairValue);
        }

        public override void Visit(Argument argument)
        {
            base.Visit(argument);
            Visit(argument.PairValue);
        }

        public override void Visit(Syntactik.DOM.Attribute attribute)
        {
            base.Visit(attribute);
            Visit(attribute.PairValue);
        }

        public override void Visit(Document document)
        {
            base.Visit(document);
            Visit(document.PairValue);
        }

        public override void Visit(Element element)
        {
            base.Visit(element);
            Visit(element.PairValue);
        }

        public override void Visit(Scope scope)
        {
            base.Visit(scope);
            Visit(scope.PairValue);
        }

        public override void Visit(Parameter parameter)
        {
            base.Visit(parameter);
            Visit(parameter.PairValue);
        }
    }
}