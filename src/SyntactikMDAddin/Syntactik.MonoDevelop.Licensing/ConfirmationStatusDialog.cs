using System;
using System.Text;
using System.Threading;
using Gtk;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;
using MonoDevelop.Core;
using Rhino.Licensing;
using Syntactik.MonoDevelop.Projects;
using Syntactik.MonoDevelop.Util;
using Timeout = System.Threading.Timeout;

namespace Syntactik.MonoDevelop.License
{
    public partial class ConfirmationStatusDialog : Dialog
    {
        public enum ConfirmationEnum
        {
            email = 1,
            license = 2
        }
        private readonly string _email;
        private readonly ConfirmationEnum _type;
        private Timer _timer;
        internal bool LicenseReceived;

        public ConfirmationStatusDialog(string email, ConfirmationEnum type)
        {
           _email = email;
            _type = type;
            // ReSharper disable once VirtualMemberCallInConstructor
            Build();
            labelEmail.Text = _email;
            if (_type == ConfirmationEnum.license)
            {
                labelLineTop.LabelProp = "Requesting license from the license server.";
                labelEmail.LabelProp = "";
                labelLineBottom.LabelProp = "";
            }

            SetLoadingState(true);
            SetConfirmationPing();
        }

        private void SetConfirmationPing()
        {
            _timer = new Timer(PingCallback, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
        }

        private void PingCallback(object state)
        {
            try
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                if (_type == ConfirmationEnum.email)
                {
                    var client = new DefaultApi(new Configuration(new ApiClient(ApiClient.Address)));
                    var confirmed = client.LeadIsConfirmed(_email);
                    if (confirmed == 2)
                    {
                        Respond(ResponseType.Ok);
                        return;
                    }
                }
                else if (_type == ConfirmationEnum.license)
                {
                    LicenseInfo licenseInfo;
                    var client = new DefaultApi(new Configuration(new ApiClient(ApiClient.Address)));
                    RequestLicense(client, _email, out licenseInfo);
                    if (licenseInfo.Uid != null && licenseInfo.Confirmed == false)
                    {
                        Runtime.RunInMainThread(delegate
                        {
                            labelLineTop.LabelProp = "License Confirmation email has been sent to:";
                            labelEmail.LabelProp = _email;
                            labelLineBottom.LabelProp = "Waiting for confirmation.";
                        });
                    }
                    else return;
                }
            }
            catch (Exception)
            {
                //ignored
            }
            _timer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public override void Destroy()
        {
            _timer.Change(-1, -1);
            _timer.Dispose();
            base.Destroy();
        }

        private void SetLoadingState(bool loading)
        {
            loaderImage.PixbufAnimation = loading ? new Gdk.PixbufAnimation(GetType().Assembly, "Syntactik.MonoDevelop.icons.24.gif") : null;
        }

        internal bool RequestLicense(DefaultApi client, string mail, out LicenseInfo licenseInfo)
        {
            var license = client.RequestLicense(mail, LicenseValidator.GetMachineId(), Environment.MachineName);
            licenseInfo = license;
            if (license.Uid == null)
            {
                DialogHelper.ShowError(licenseInfo.ErrorMessage, this);
                Respond(ResponseType.Cancel);
                return false;
            }
            if (licenseInfo.Confirmed == false)
            {
                return false;
            }

            var licenseBody = license.License;
            System.IO.File.WriteAllText(SyntactikProject.GetLicenseFileName(), licenseBody);
            var lic = new Licensing.License(SyntactikProject.GetLicenseFileName());
            string expiration;
            string type;
            try
            {
                lic.ValidateLicense();
                expiration = lic.Validator.ExpirationDate.ToShortDateString();
                type = lic.Validator.LicenseType.ToString();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError("Invalid License Key " + ex.Message, this);
                Respond(ResponseType.Cancel);
                return false;
            }

            var message = new StringBuilder();
            message.AppendLine("License is Valid.");
            message.AppendLine("License Type: " + type);
            message.AppendLine("Issued to: " + mail);
            message.AppendLine("Valid till: " + expiration);
            DialogHelper.ShowMessage(message.ToString(), this);
            Respond(ResponseType.Ok);
            return true;
        }
    }
}
