using System.Collections.Generic;
using Syntactik.DOM;

namespace Syntactik.MonoDevelop.Completion.DOM
{
    public class Attribute : Syntactik.DOM.Mapped.Attribute
    {
        public override void AppendChild(Pair child)
        {
            if (Delimiter == DelimiterEnum.EC)
            {
                InterpolationItems = new List<object> {child};
                child.InitializeParent(this);
            }
            else
                base.AppendChild(child);
        }
    }
}
