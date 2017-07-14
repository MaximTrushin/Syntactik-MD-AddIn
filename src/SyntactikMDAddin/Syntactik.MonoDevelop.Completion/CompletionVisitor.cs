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

        public override void OnAlias(Alias alias)
        {
            base.OnAlias(alias);
            Visit(alias.PairValue);
        }

        public override void OnAliasDefinition(AliasDefinition aliasDefinition)
        {
            base.OnAliasDefinition(aliasDefinition);
            Visit(aliasDefinition.PairValue);
        }

        public override void OnArgument(Argument argument)
        {
            base.OnArgument(argument);
            Visit(argument.PairValue);
        }

        public override void OnAttribute(Syntactik.DOM.Attribute attribute)
        {
            base.OnAttribute(attribute);
            Visit(attribute.PairValue);
        }

        public override void OnDocument(Document document)
        {
            base.OnDocument(document);
            Visit(document.PairValue);
        }

        public override void OnElement(Element element)
        {
            base.OnElement(element);
            Visit(element.PairValue);
        }

        public override void OnScope(Scope scope)
        {
            base.OnScope(scope);
            Visit(scope.PairValue);
        }

        public override void OnParameter(Parameter parameter)
        {
            base.OnParameter(parameter);
            Visit(parameter.PairValue);
        }
    }
}