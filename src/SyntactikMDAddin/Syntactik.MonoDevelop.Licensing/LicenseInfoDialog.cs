using System;
using Gtk;
using Syntactik.MonoDevelop.Projects;
using Syntactik.MonoDevelop.Util;

namespace Syntactik.MonoDevelop.License
{
    public partial class LicenseInfoDialog : Dialog
    {
        public LicenseInfoDialog()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            Build();
            btnRequest.Clicked += OnBtnRequestClick;
            var col = new Gdk.Color(240, 240, 240);
            entryEmail.ModifyBase(StateType.Normal, col);
            entryName.ModifyBase(StateType.Normal, col);
            entryCompany.ModifyBase(StateType.Normal, col);
            entryPosition.ModifyBase(StateType.Normal, col);
            entryLicenseId.ModifyBase(StateType.Normal, col);
            entryLicenseType.ModifyBase(StateType.Normal, col);
            entryExpires.ModifyBase(StateType.Normal, col);

            ValidateLicense();
        }

        private void ValidateLicense()
        {
            var lic = new Licensing.License(SyntactikProject.GetLicenseFileName());
            try
            {
                lic.ValidateLicense();
                entryEmail.Text = lic.Validator.LicenseAttributes["Email"];
                entryName.Text = lic.Validator.Name;
                entryCompany.Text = lic.Validator.LicenseAttributes["Company"];
                entryPosition.Text = lic.Validator.LicenseAttributes["Position"];
                entryExpires.Text = lic.Validator.ExpirationDate.ToShortDateString();
                entryLicenseType.Text = lic.Validator.LicenseType.ToString();
                entryLicenseId.Text = lic.Validator.UserId.ToString();
                btnRequest.Sensitive = false;
                btnClose.Label = "Ok";
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void OnBtnRequestClick(object sender, EventArgs e)
        {

            using (var dlg = new LicenseRequestDialog())
            {
                var res = (ResponseType)DialogHelper.ShowCustomDialog(dlg, this);
                if (res != ResponseType.Ok) return;
                ValidateLicense();
            }
        }
    }
}
