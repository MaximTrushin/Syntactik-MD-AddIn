using MonoDevelop.Components;
using MonoDevelop.Ide.WelcomePage;

namespace Syntactik.MonoDevelop.WelcomePage
{
    public class WelcomePageProvider: IWelcomePageProvider
    {
        public Control CreateWidget()
        {
            return new SyntactikWelcomePage();
        }
    }
}