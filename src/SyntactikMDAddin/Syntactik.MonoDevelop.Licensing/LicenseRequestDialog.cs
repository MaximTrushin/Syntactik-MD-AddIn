using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Gtk;
using IO.Swagger.Api;
using IO.Swagger.Client;
using MonoDevelop.Core;
using Rhino.Licensing;
using Syntactik.MonoDevelop.Util;
using Dialog = Gtk.Dialog;

namespace Syntactik.MonoDevelop.License
{
    partial class LicenseRequestDialog : Dialog
    {
        internal bool EmailConfirmed => LeadState == 2;
        /// <summary>
        /// 0 - New Lead, 1 - Existing with unconfirmed email, 2 - Existing with confirmed email
        /// </summary>
        private int? LeadState { get; set; }
        private string _email;

        public LicenseRequestDialog()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            Build();
            entryEmail.Changed += OnEmailChanged;
            buttonRequest.Clicked += BtnRequestOnClicked;
            entryName.Sensitive = false;
            entryCompany.Sensitive = false;
            entryPosition.Sensitive = false;
            buttonRequest.Sensitive = false;
        }

        private void OnEmailChanged(object sender, EventArgs e)
        {
            LeadState = null;
            DisableNonEmailEntries();
            buttonRequest.Sensitive = IsValidEmail(entryEmail.Text);
        }

        private void BtnRequestOnClicked(object sender, EventArgs e)
        {
            if (LeadState == null)
            {
                //Checking if email is already registered
                Task.Run(() => UpdateLeadState()).ContinueWith(task => ProcessLeadState(true));
            }
            else ProcessLeadState();
        }

        private void ProcessLeadState(bool newUser = false)
        {
            switch (LeadState)
            {
                case 0:
                    if (newUser)
                    {
                        Application.Invoke(delegate
                        {
                            DialogHelper.ShowMessage("You are requesting the license first time. To get the license, please enter your full name, company and position.", this);
                            EnableNonEmailEntries();
                            entryName.HasFocus = true;
                        });
                        break;
                    }
                    //Valid new email. Customer has to enter full name, company and position
                    if (string.IsNullOrWhiteSpace(entryName.Text))
                    {
                        Application.Invoke(delegate{
                                DialogHelper.ShowWarning("Please enter your full name.", this);
                                entryName.HasFocus = true;
                        });
                        return;
                    }
                    Task.Run(() => SignupLead(entryEmail.Text, entryName.Text, entryPosition.Text, entryCompany.Text))
                        .ContinueWith(task => ProcessLeadState());
                    break;
                case 1:
                    Runtime.RunInMainThread(delegate
                    {
                        SetLoadingState(true);
                        using (
                            var dlg = new ConfirmationStatusDialog(entryEmail.Text,
                                ConfirmationStatusDialog.ConfirmationEnum.email))
                        {
                            var result = DialogHelper.ShowCustomDialog(dlg, this);
                            SetLoadingState(false);
                            if (result != (int) ResponseType.Ok) return;
                            LeadState = 2;
                        }
                    }).ContinueWith(task =>
                    {
                        if (LeadState == 2)
                            Application.Invoke(delegate
                            {
                                ProcessLeadState();
                            });
                    });
                    break;
                case 2:
                    Application.Invoke(delegate
                    {
                        SetLoadingState(true);
                        using (
                            var dlg = new ConfirmationStatusDialog(entryEmail.Text, ConfirmationStatusDialog.ConfirmationEnum.license))
                        {
                            var result = DialogHelper.ShowCustomDialog(dlg, this);
                            SetLoadingState(false);
                            if (result == (int) ResponseType.Ok)
                            {
                                Respond(ResponseType.Ok);
                                Destroy();
                                return;
                            }
                        }
                    });
                    break; 
            }
        }

        private void SignupLead(string entryEmailText, string entryNameText, string entryPositionText, string entryCompanyText)
        {
            Application.Invoke(delegate {
                SetLoadingState(true);
            });
            try
            {
                var client = new DefaultApi(new Configuration(new ApiClient(ApiClient.Address)));
                client.SignupLead(entryEmailText, entryNameText, entryPositionText, entryCompanyText,
                    LicenseValidator.GetMachineId());
                LeadState = 1;
            }
            catch (Exception)
            {
                DialogHelper.ShowError("Something went wrong. Please try again.", this);
            }
            finally
            {
                Application.Invoke(delegate {
                    SetLoadingState(false);
                });
            }
        }

        private void SetLoadingState(bool loading)
        {
            loaderImage.PixbufAnimation = loading ? new Gdk.PixbufAnimation(GetType().Assembly, "Syntactik.MonoDevelop.icons.24.gif") : null;
            entryEmail.Sensitive = loading == false;
            buttonCancel.Sensitive = loading == false;
            buttonRequest.Sensitive = loading == false;

            if (loading)
            {
                DisableNonEmailEntries();
            }
            else if (LeadState == 0)
            {
                EnableNonEmailEntries();
            }
        }


        private void DisableNonEmailEntries()
        {
            entryName.Sensitive = false;
            entryCompany.Sensitive = false;
            entryPosition.Sensitive = false;
        }

        private void EnableNonEmailEntries()
        {
            entryName.Sensitive = true;
            entryCompany.Sensitive = true;
            entryPosition.Sensitive = true;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                // ReSharper disable once UnusedVariable
                var mailAddress = new MailAddress(email);
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        private void UpdateLeadState()
        {
            Application.Invoke(delegate {
                SetLoadingState(true);
            });
            _email = entryEmail.Text;
            try
            {
                var client = new DefaultApi(new Configuration(new ApiClient(ApiClient.Address)));
                LeadState = client.LeadIsConfirmed(_email);
            }
            catch (Exception)
            {
                Application.Invoke(delegate {
                    DialogHelper.ShowError("Invalid email.", this);
                    entryEmail.HasFocus = true;
                });
            }
            finally
            {
                Application.Invoke(delegate {
                    SetLoadingState(false);
                });
            }
        }
    }
}