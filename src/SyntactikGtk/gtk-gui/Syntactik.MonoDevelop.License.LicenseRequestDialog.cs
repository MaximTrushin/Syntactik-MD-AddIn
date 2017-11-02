
// This file has been generated by the GUI designer. Do not modify.
namespace Syntactik.MonoDevelop.License
{
	public partial class LicenseRequestDialog
	{
		private global::Gtk.Label label5;

		private global::Gtk.VBox vbox2;

		private global::Gtk.HSeparator hseparator8;

		private global::Gtk.Label label1;

		private global::Gtk.Entry entryEmail;

		private global::Gtk.Label label2;

		private global::Gtk.Entry entryName;

		private global::Gtk.Label label3;

		private global::Gtk.Entry entryCompany;

		private global::Gtk.Label label4;

		private global::Gtk.Entry entryPosition;

		private global::Gtk.Label label6;

		private global::Gtk.Image loaderImage;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonRequest;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget Syntactik.MonoDevelop.License.LicenseRequestDialog
			this.Name = "Syntactik.MonoDevelop.License.LicenseRequestDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Early Access License Request");
			this.Icon = global::Stetic.IconLoader.LoadIcon(this, "gtk-dialog-authentication", global::Gtk.IconSize.Menu);
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.Modal = true;
			this.BorderWidth = ((uint)(5));
			this.Resizable = false;
			this.SkipPagerHint = true;
			this.SkipTaskbarHint = true;
			// Internal child Syntactik.MonoDevelop.License.LicenseRequestDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.label5 = new global::Gtk.Label();
			this.label5.Name = "label5";
			this.label5.Xalign = 0F;
			this.label5.LabelProp = global::Mono.Unix.Catalog.GetString("Please enter your email to get the Early Access license.\n\nIf you are requesting t" +
					"he license first time, you will be asked to enter your full name, company and po" +
					"sition.\n");
			this.label5.Wrap = true;
			w1.Add(this.label5);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(w1[this.label5]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 2;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hseparator8 = new global::Gtk.HSeparator();
			this.hseparator8.Name = "hseparator8";
			this.vbox2.Add(this.hseparator8);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hseparator8]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Email *");
			this.vbox2.Add(this.label1);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label1]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.entryEmail = new global::Gtk.Entry();
			this.entryEmail.CanFocus = true;
			this.entryEmail.Name = "entryEmail";
			this.entryEmail.IsEditable = true;
			this.entryEmail.InvisibleChar = '●';
			this.vbox2.Add(this.entryEmail);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.entryEmail]));
			w5.Position = 2;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Full Name *");
			this.vbox2.Add(this.label2);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label2]));
			w6.Position = 3;
			w6.Expand = false;
			w6.Fill = false;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.entryName = new global::Gtk.Entry();
			this.entryName.CanFocus = true;
			this.entryName.Name = "entryName";
			this.entryName.IsEditable = true;
			this.entryName.InvisibleChar = '●';
			w1.Add(this.entryName);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(w1[this.entryName]));
			w8.Position = 2;
			w8.Expand = false;
			w8.Fill = false;
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Company");
			w1.Add(this.label3);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(w1[this.label3]));
			w9.Position = 3;
			w9.Expand = false;
			w9.Fill = false;
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.entryCompany = new global::Gtk.Entry();
			this.entryCompany.CanFocus = true;
			this.entryCompany.Name = "entryCompany";
			this.entryCompany.IsEditable = true;
			this.entryCompany.InvisibleChar = '●';
			w1.Add(this.entryCompany);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(w1[this.entryCompany]));
			w10.Position = 4;
			w10.Expand = false;
			w10.Fill = false;
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("Position");
			w1.Add(this.label4);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(w1[this.label4]));
			w11.Position = 5;
			w11.Expand = false;
			w11.Fill = false;
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.entryPosition = new global::Gtk.Entry();
			this.entryPosition.CanFocus = true;
			this.entryPosition.Name = "entryPosition";
			this.entryPosition.IsEditable = true;
			this.entryPosition.InvisibleChar = '●';
			w1.Add(this.entryPosition);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(w1[this.entryPosition]));
			w12.Position = 6;
			w12.Expand = false;
			w12.Fill = false;
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.label6 = new global::Gtk.Label();
			this.label6.Name = "label6";
			this.label6.Xalign = 0F;
			this.label6.LabelProp = global::Mono.Unix.Catalog.GetString("* - required field");
			w1.Add(this.label6);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(w1[this.label6]));
			w13.Position = 7;
			w13.Expand = false;
			w13.Fill = false;
			// Internal child Syntactik.MonoDevelop.License.LicenseRequestDialog.ActionArea
			global::Gtk.HButtonBox w14 = this.ActionArea;
			w14.Name = "dialog1_ActionArea";
			w14.Spacing = 10;
			w14.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.loaderImage = new global::Gtk.Image();
			this.loaderImage.WidthRequest = 100;
			this.loaderImage.HeightRequest = 25;
			this.loaderImage.Name = "loaderImage";
			this.loaderImage.Xalign = 0F;
			w14.Add(this.loaderImage);
			global::Gtk.ButtonBox.ButtonBoxChild w15 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w14[this.loaderImage]));
			w15.Expand = false;
			w15.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = global::Mono.Unix.Catalog.GetString("_Cancel");
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w16 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w14[this.buttonCancel]));
			w16.Position = 1;
			w16.Expand = false;
			w16.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonRequest = new global::Gtk.Button();
			this.buttonRequest.TooltipMarkup = "Check email";
			this.buttonRequest.CanFocus = true;
			this.buttonRequest.Name = "buttonRequest";
			this.buttonRequest.Label = global::Mono.Unix.Catalog.GetString("Request License");
			w14.Add(this.buttonRequest);
			global::Gtk.ButtonBox.ButtonBoxChild w17 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w14[this.buttonRequest]));
			w17.Position = 2;
			w17.Expand = false;
			w17.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 379;
			this.DefaultHeight = 356;
			this.Hide();
		}
	}
}
