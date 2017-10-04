using System;
using System.Threading;
using Gtk;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;
using MonoDevelop.Ide;
using Rhino.Licensing;
using Syntactik.MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.License
{
    public sealed partial class LicenseInfoDialog : Dialog
    {
        private bool _needConfirm;
        private string _confirmMessage;
        private readonly Timer _timer;

        public LicenseInfoDialog()
        {
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

            _timer = new Timer(this.Callback, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));

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

        private void Callback(object state)
        {
            if (_needConfirm)
            {
                var client = new DefaultApi(new Configuration(new ApiClient(ApiClient.Address)));
                try
                {
                    var confirmed = client.LeadIsConfirmed(entryEmail.Text);
                    if (confirmed == 2)
                    {
                        var li = client.RequestLicense(entryEmail.Text, LicenseValidator.GetMachineId(),
                            Environment.MachineName);
                        if (li.Confirmed == true)
                        {
                            _timer.Change(-1, -1);
                            _needConfirm = false;
                            MessageService.ShowMessage(_confirmMessage);

                            LicenseInfo licenseInfo;
                            LicenseRequestDialog.ValidateLicense(client, entryEmail.Text, out licenseInfo);
                            DisplayInfo(licenseInfo);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }


        protected override void OnClose()
        {
            _timer.Change(-1, -1);
            _timer.Dispose();
            base.OnClose();
        }

        private void OnBtnRequestClick(object sender, EventArgs e)
        {

            using (var dlg = new LicenseRequestDialog())
            {
                var res = (ResponseType) MessageService.ShowCustomDialog(dlg);
                if (res == ResponseType.Ok)
                {
                    entryEmail.Text = dlg.Info.Email;
                    entryName.Text = dlg.Info.FullName;
                    entryCompany.Text = dlg.Info.Company;
                    entryPosition.Text = dlg.Info.Position;
                    //_needConfirm = dlg.EmailConfirmed == false || dlg.LicenseInfo.Confirmed == false;
                    btnRequest.Sensitive = false;

                    if (dlg.LicenseInfo != null && dlg.LicenseInfo.Confirmed != null && (bool) dlg.LicenseInfo.Confirmed)
                    {
                        //License = dlg.LicenseInfo;
                        DisplayInfo(dlg.LicenseInfo);
                    }

                    _confirmMessage = "Email confirmed!";
                    if (dlg.LicenseInfo != null && dlg.LicenseInfo.Confirmed != null &&
                        (bool) !dlg.LicenseInfo.Confirmed)
                        _confirmMessage = "License confirmed!";

                }
            }
        }

        private void DisplayInfo(LicenseInfo info)
        {
            if (info != null)
            {
                entryLicenseId.Text = info.Uid;
                if (info.ExpiresAt != null) entryExpires.Text = info.ExpiresAt.Value.ToShortDateString();
                entryLicenseType.Text = info.Type;
                btnClose.Label = "Ok";
            }
        }
    }
}
