﻿using System;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Gtk;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;
using MonoDevelop.Ide;
using Rhino.Licensing;
using Syntactik.MonoDevelop.Highlighting;
using Syntactik.MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.License
{
    internal class Info
    {
        public string Email;
        public string FullName;
        public string Company;
        public string Position;
    }

    public partial class LicenseRequestDialog : Gtk.Dialog
    {
        internal Info Info { get; } = new Info();
        internal bool EmailConfirmed { get; private set; }
        private int? LeadState { get; set; }
        internal LicenseInfo LicenseInfo { get; private set; }

        public LicenseRequestDialog()
        {
            Build();
            SetLoadingState(false);
            entryEmail.FocusOutEvent += this.OnEmailEntered;
            buttonCancel.Clicked += this.BtnCancelClicked;
            buttonRequest.Clicked += this.BtnRequestOnClicked;
        }

        private void SetLoadingState(bool loading)
        {
            var m = typeof(SyntactikSyntaxMode).Assembly.GetManifestResourceNames();
            loaderImage.PixbufAnimation = loading ? new Gdk.PixbufAnimation(this.GetType().Assembly, "Syntactik.MonoDevelop.icons.24.gif") : null;
            entryEmail.Sensitive = loading == false;
            entryName.Sensitive = loading == false;
            entryCompany.Sensitive = loading == false;
            entryPosition.Sensitive = loading == false;
            buttonRequest.Sensitive = loading == false;
            buttonCancel.Sensitive = loading == false;
        }

        private string _email;

        protected void OnEmailEntered(object o, FocusOutEventArgs args)
        {
            if (_email == entryEmail.Text || !IsValid(entryEmail.Text))
                return;

            var task = new Task(DoCompletions);
            task.Start();
        }

        private bool IsValid(string email)
        {
            try
            {
                var mailAddress = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void DoCompletions()
        {
            _email = entryEmail.Text;
            SetLoadingState(true);
            try
            {
                UpdateLeadState(_email, true);

                if (LeadState > 0)
                {
                    entryCompany.Text = "●●●●●●";
                    entryName.Text = "●●●●●●";
                    entryPosition.Text = "●●●●●●";

                    entryCompany.IsEditable = false;
                    entryName.IsEditable = false;
                    entryPosition.IsEditable = false;
                }
                else
                {
                    entryCompany.Text = "";
                    entryName.Text = "";
                    entryPosition.Text = "";
                    entryCompany.IsEditable = true;
                    entryName.IsEditable = true;
                    entryPosition.IsEditable = true;
                }
            }
            catch (Exception ex)
            {
                MessageService.ShowError("Error requesting license: " + ex.Message, "Error");
                KeepAbove = true;
            }
            finally
            {
                SetLoadingState(false);
            }
        }


        private void UpdateLeadState(string mail, bool force = false)
        {
            if (LeadState == null || force)
            {
                var client = new DefaultApi(new Configuration(new ApiClient(ApiClient.Address)));

                LeadState = client.LeadIsConfirmed(mail);
                EmailConfirmed = LeadState == 2;
            }
        }

        private void DoRequest()
        {
            string mail = entryEmail.Text;
            Info.Email = mail;
            Info.FullName = entryName.Text;
            Info.Company = entryCompany.Text;
            Info.Position = entryPosition.Text;

            if (!IsValid(mail))
            {
                MessageService.ShowError("Invalid email address.");
                this.KeepAbove = true;
                return;
            }
            string fullName = entryName.Text;
            if (string.IsNullOrEmpty(fullName) && !EmailConfirmed)
            {
                MessageService.ShowError("Full Name required");
                this.KeepAbove = true;
                return;
            }

            SetLoadingState(true);
            try
            {
                UpdateLeadState(mail);
                var client = new DefaultApi(new Configuration(new ApiClient(ApiClient.Address)));
                if (LeadState == 0) //Lead doesn't exist
                {
                    string company = entryCompany.Text;
                    string position = entryPosition.Text;

                    client.SignupLead(mail, fullName, position, company, LicenseValidator.GetMachineId());
                    LeadState = 1;
                }
                EmailConfirmed = LeadState == 2;
                if (LeadState == 1)
                {
                    MessageService.ShowMessage("Confirmation email has been sent to " + mail);
                    this.DefaultResponse = ResponseType.Ok;
                    this.Destroy();
                    return;
                }

                LicenseInfo licenseInfo;

                bool valid = ValidateLicense(client, mail, out licenseInfo);
                LicenseInfo = licenseInfo;
                if (licenseInfo.Uid != null && licenseInfo.Confirmed == false)
                {
                    MessageService.ShowMessage("License Confirmation email has been sent to " + mail);
                    this.DefaultResponse = ResponseType.Ok;
                    this.Destroy();
                    return;
                }

                if (valid)
                {
                    this.DefaultResponse = ResponseType.Ok;
                    this.Destroy();
                }
            }
            catch (Exception ex)
            {
                MessageService.ShowError("Error requesting license: " + ex.Message);
                KeepAbove = true;
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        internal static bool ValidateLicense(DefaultApi client, string mail, out LicenseInfo licenseInfo)
        {
            var license = client.RequestLicense(mail, LicenseValidator.GetMachineId(), Environment.MachineName);
            licenseInfo = license;
            if (license.Uid == null)
            {
                MessageService.ShowError(license.ErrorMessage);
                return false;
            }
            if (licenseInfo.Confirmed == false)
                return false;

            var licenseBody = license.License;
            System.IO.File.WriteAllText(SyntactikProject.GetLicenseFileName(), licenseBody);
            var lic = new Licensing.License(SyntactikProject.GetLicenseFileName());
            bool valid;
            string expiration = string.Empty;
            string type = string.Empty;
            string errorMessage = string.Empty;
            try
            {
                lic.ValidateLicense();
                valid = true;
                expiration = lic.Validator.ExpirationDate.ToShortDateString();
                type = lic.Validator.LicenseType.ToString();
            }
            catch (Exception ex)
            {
                valid = false;
                errorMessage = ex.Message;
            }

            if (valid)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine("License Valid.");
                message.AppendLine("License Type: " + type);
                message.AppendLine("Issued to: " + mail);
                message.AppendLine("Valid till: " + expiration);
                MessageService.ShowMessage(message.ToString());
                return true;
            }
            MessageService.ShowError("Invalid License Key " + errorMessage);
            return false;
        }

        protected void BtnRequestOnClicked(object sender, EventArgs e)
        {
            this.KeepAbove = false;
            var task = new Task(DoRequest);
            task.Start();
        }

        protected void BtnCancelClicked(object sender, EventArgs e)
        {
            this.DefaultResponse = ResponseType.Cancel;
            this.Destroy();
        }
    }
}