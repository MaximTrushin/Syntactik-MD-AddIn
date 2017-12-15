using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide.WelcomePage;

namespace Syntactik.MonoDevelop.WelcomePage
{
    class SyntactikWelcomePage : WelcomePageWidget
    {
        protected override void BuildContent(Container parent)
        {

            LogoImage = Xwt.Drawing.Image.FromFile(BrandingService.GetFile("welcome-logo.png"));
            TopBorderImage = Xwt.Drawing.Image.FromFile(BrandingService.GetFile("welcome-tile.png"));

            var mainAlignment = new Gtk.Alignment(0.5f, 0.5f, 0f, 1f);
            var mainCol = new WelcomePageColumn();
            mainAlignment.Add(mainCol);

            var row1 = new WelcomePageRow();
            row1.PackStart(new WelcomePageButtonBar(
                new WelcomePageBarButton("Syntactik.com", "http://www.syntactik.com", "welcome-link-md-16.png"),
                new WelcomePageBarButton(GettextCatalog.GetString("Documentation"), "https://github.com/syntactik/Syntactik/blob/master/README.md", "welcome-link-info-16.png"),
                new WelcomePageBarButton(GettextCatalog.GetString("Support"), "https://gitter.im/syntactik/Syntactik", "welcome-link-support-16.png")
                )
            );
            mainCol.PackStart(row1, false, false, 0);

            var row2 = new WelcomePageRow(
                new WelcomePageColumn(
                new WelcomePageRecentProjectsList(GettextCatalog.GetString("Solutions"))
                ),
                new WelcomePageColumn(
                    new WelcomePageNewsFeed(GettextCatalog.GetString("Syntactik News"), "http://software.xamarin.com/Service/News", "NewsLinks")
                ),
                new WelcomePageColumn(
                    new WelcomePageTipOfTheDaySection()
                )
            );
            mainCol.PackStart(row2, false, false, 0);

            parent.Add(mainAlignment);
        }
    }
}
