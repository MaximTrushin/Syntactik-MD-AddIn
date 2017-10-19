using Gtk;
using MonoDevelop.Ide.Fonts;

namespace Syntactik.MonoDevelop.DisplayBinding
{
    class LanguageItemWindow : global::MonoDevelop.Components.TooltipWindow
    {
        public bool IsEmpty { get; set; }

        public LanguageItemWindow(string errorInformations)
        {
            var tooltip = errorInformations;
            
            if (string.IsNullOrEmpty(tooltip) || tooltip == "?")
            {
                IsEmpty = true;
                return;
            }

            var label = new global::MonoDevelop.Components.FixedWidthWrapLabel()
            {
                Wrap = Pango.WrapMode.WordChar,
                Indent = -20,
                BreakOnCamelCasing = true,
                BreakOnPunctuation = true,
                Markup = tooltip,
            };
            BorderWidth = 3;
            Add(label);
            UpdateFont(label);

            EnableTransparencyControl = true;
        }

        //return the real width
        public int SetMaxWidth(int maxWidth)
        {
            var label = Child as global::MonoDevelop.Components.FixedWidthWrapLabel;
            if (label == null)
                return Allocation.Width;
            label.MaxWidth = maxWidth;
            return label.RealWidth;
        }

        protected override void OnStyleSet(Style previous_style)
        {
            base.OnStyleSet(previous_style);
            UpdateFont(Child as global::MonoDevelop.Components.FixedWidthWrapLabel);
        }

        void UpdateFont(global::MonoDevelop.Components.FixedWidthWrapLabel label)
        {
            if (label == null)
                return;
            label.FontDescription = FontService.GetFontDescription("Pad");

        }
    }
}
