
// This file has been generated by the GUI designer. Do not modify.
namespace Syntactik.MonoDevelop.License
{
	public partial class LicenseInfo
	{
		private global::Gtk.VBox vbox3;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label label4;

		private global::Gtk.VSeparator vseparator1;

		private global::Gtk.VButtonBox vbuttonbox1;

		private global::Gtk.Button button17;

		private global::Gtk.HSeparator hseparator2;

		private global::Gtk.Frame frame2;

		private global::Gtk.Alignment GtkAlignment2;

		private global::Gtk.Table table1;

		private global::Gtk.Entry entry8;

		private global::Gtk.Entry entryCompany;

		private global::Gtk.Entry entryEmail;

		private global::Gtk.Entry entryLicenseId;

		private global::Gtk.Entry entryLicenseType;

		private global::Gtk.Entry entryName;

		private global::Gtk.Entry entryPosition;

		private global::Gtk.HSeparator hseparator1;

		private global::Gtk.Label label2;

		private global::Gtk.Label label3;

		private global::Gtk.Label label5;

		private global::Gtk.Label label6;

		private global::Gtk.Label label7;

		private global::Gtk.Label label8;

		private global::Gtk.Label label9;

		private global::Gtk.Label GtkLabel3;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget Syntactik.MonoDevelop.License.LicenseInfo
			this.Name = "Syntactik.MonoDevelop.License.LicenseInfo";
			this.Title = global::Mono.Unix.Catalog.GetString("Syntactik Software Activation");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.Modal = true;
			this.Resizable = false;
			this.AllowGrow = false;
			// Internal child Syntactik.MonoDevelop.License.LicenseInfo.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label4 = new global::Gtk.Label();
			this.label4.TooltipMarkup = @"Thank you for your interest in Syntactik. Syntactik editor is available only in the Early Access mode. Please register using the button ""Request Early Access"". Please visit the web site <a href=""www.syntactik.com"">www.syntactik.com<a/> to get more information about Syntactik language and editor.";
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString(@"
Thank you for your interest in Syntactik. 

Syntactik editor is available only in the Early Access mode. Please register using the button ""Request Early Access"". 

Please visit the web site www.syntactik.com to get more information about Syntactik language and editor.
");
			this.label4.UseMarkup = true;
			this.label4.Wrap = true;
			this.label4.WidthChars = 65;
			this.hbox1.Add(this.label4);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label4]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			w2.Padding = ((uint)(4));
			// Container child hbox1.Gtk.Box+BoxChild
			this.vseparator1 = new global::Gtk.VSeparator();
			this.vseparator1.Name = "vseparator1";
			this.hbox1.Add(this.vseparator1);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vseparator1]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbuttonbox1 = new global::Gtk.VButtonBox();
			this.vbuttonbox1.Name = "vbuttonbox1";
			this.vbuttonbox1.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(2));
			// Container child vbuttonbox1.Gtk.ButtonBox+ButtonBoxChild
			this.button17 = new global::Gtk.Button();
			this.button17.CanFocus = true;
			this.button17.Name = "button17";
			this.button17.UseUnderline = true;
			this.button17.BorderWidth = ((uint)(10));
			this.button17.Label = global::Mono.Unix.Catalog.GetString("Request Early Access2");
			this.vbuttonbox1.Add(this.button17);
			global::Gtk.ButtonBox.ButtonBoxChild w4 = ((global::Gtk.ButtonBox.ButtonBoxChild)(this.vbuttonbox1[this.button17]));
			w4.Expand = false;
			w4.Fill = false;
			this.hbox1.Add(this.vbuttonbox1);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbuttonbox1]));
			w5.Position = 2;
			w5.Expand = false;
			w5.Fill = false;
			this.vbox3.Add(this.hbox1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.hbox1]));
			w6.Position = 0;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.hseparator2 = new global::Gtk.HSeparator();
			this.hseparator2.Name = "hseparator2";
			this.vbox3.Add(this.hseparator2);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.hseparator2]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.frame2 = new global::Gtk.Frame();
			this.frame2.Name = "frame2";
			this.frame2.ShadowType = ((global::Gtk.ShadowType)(0));
			// Container child frame2.Gtk.Container+ContainerChild
			this.GtkAlignment2 = new global::Gtk.Alignment(0F, 0F, 1F, 1F);
			this.GtkAlignment2.Name = "GtkAlignment2";
			this.GtkAlignment2.LeftPadding = ((uint)(12));
			// Container child GtkAlignment2.Gtk.Container+ContainerChild
			this.table1 = new global::Gtk.Table(((uint)(8)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.entry8 = new global::Gtk.Entry();
			this.entry8.CanFocus = true;
			this.entry8.Name = "entry8";
			this.entry8.IsEditable = true;
			this.entry8.InvisibleChar = '●';
			this.table1.Add(this.entry8);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.table1[this.entry8]));
			w8.TopAttach = ((uint)(7));
			w8.BottomAttach = ((uint)(8));
			w8.LeftAttach = ((uint)(1));
			w8.RightAttach = ((uint)(2));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.entryCompany = new global::Gtk.Entry();
			this.entryCompany.CanFocus = true;
			this.entryCompany.Name = "entryCompany";
			this.entryCompany.IsEditable = true;
			this.entryCompany.InvisibleChar = '●';
			this.table1.Add(this.entryCompany);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.table1[this.entryCompany]));
			w9.TopAttach = ((uint)(2));
			w9.BottomAttach = ((uint)(3));
			w9.LeftAttach = ((uint)(1));
			w9.RightAttach = ((uint)(2));
			w9.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.entryEmail = new global::Gtk.Entry();
			this.entryEmail.CanFocus = true;
			this.entryEmail.Name = "entryEmail";
			this.entryEmail.IsEditable = true;
			this.entryEmail.InvisibleChar = '●';
			this.table1.Add(this.entryEmail);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.table1[this.entryEmail]));
			w10.LeftAttach = ((uint)(1));
			w10.RightAttach = ((uint)(2));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.entryLicenseId = new global::Gtk.Entry();
			this.entryLicenseId.CanFocus = true;
			this.entryLicenseId.Name = "entryLicenseId";
			this.entryLicenseId.IsEditable = true;
			this.entryLicenseId.InvisibleChar = '●';
			this.table1.Add(this.entryLicenseId);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.table1[this.entryLicenseId]));
			w11.TopAttach = ((uint)(5));
			w11.BottomAttach = ((uint)(6));
			w11.LeftAttach = ((uint)(1));
			w11.RightAttach = ((uint)(2));
			w11.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.entryLicenseType = new global::Gtk.Entry();
			this.entryLicenseType.CanFocus = true;
			this.entryLicenseType.Name = "entryLicenseType";
			this.entryLicenseType.IsEditable = true;
			this.entryLicenseType.InvisibleChar = '●';
			this.table1.Add(this.entryLicenseType);
			global::Gtk.Table.TableChild w12 = ((global::Gtk.Table.TableChild)(this.table1[this.entryLicenseType]));
			w12.TopAttach = ((uint)(6));
			w12.BottomAttach = ((uint)(7));
			w12.LeftAttach = ((uint)(1));
			w12.RightAttach = ((uint)(2));
			w12.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.entryName = new global::Gtk.Entry();
			this.entryName.CanFocus = true;
			this.entryName.Name = "entryName";
			this.entryName.IsEditable = true;
			this.entryName.InvisibleChar = '●';
			this.table1.Add(this.entryName);
			global::Gtk.Table.TableChild w13 = ((global::Gtk.Table.TableChild)(this.table1[this.entryName]));
			w13.TopAttach = ((uint)(1));
			w13.BottomAttach = ((uint)(2));
			w13.LeftAttach = ((uint)(1));
			w13.RightAttach = ((uint)(2));
			w13.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.entryPosition = new global::Gtk.Entry();
			this.entryPosition.CanFocus = true;
			this.entryPosition.Name = "entryPosition";
			this.entryPosition.IsEditable = true;
			this.entryPosition.InvisibleChar = '●';
			this.table1.Add(this.entryPosition);
			global::Gtk.Table.TableChild w14 = ((global::Gtk.Table.TableChild)(this.table1[this.entryPosition]));
			w14.TopAttach = ((uint)(3));
			w14.BottomAttach = ((uint)(4));
			w14.LeftAttach = ((uint)(1));
			w14.RightAttach = ((uint)(2));
			w14.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.hseparator1 = new global::Gtk.HSeparator();
			this.hseparator1.Name = "hseparator1";
			this.table1.Add(this.hseparator1);
			global::Gtk.Table.TableChild w15 = ((global::Gtk.Table.TableChild)(this.table1[this.hseparator1]));
			w15.TopAttach = ((uint)(4));
			w15.BottomAttach = ((uint)(5));
			w15.RightAttach = ((uint)(2));
			w15.XOptions = ((global::Gtk.AttachOptions)(4));
			w15.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Name");
			this.table1.Add(this.label2);
			global::Gtk.Table.TableChild w16 = ((global::Gtk.Table.TableChild)(this.table1[this.label2]));
			w16.TopAttach = ((uint)(1));
			w16.BottomAttach = ((uint)(2));
			w16.XOptions = ((global::Gtk.AttachOptions)(4));
			w16.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Company");
			this.table1.Add(this.label3);
			global::Gtk.Table.TableChild w17 = ((global::Gtk.Table.TableChild)(this.table1[this.label3]));
			w17.TopAttach = ((uint)(2));
			w17.BottomAttach = ((uint)(3));
			w17.XOptions = ((global::Gtk.AttachOptions)(4));
			w17.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label5 = new global::Gtk.Label();
			this.label5.Name = "label5";
			this.label5.Xalign = 0F;
			this.label5.LabelProp = global::Mono.Unix.Catalog.GetString("Position");
			this.table1.Add(this.label5);
			global::Gtk.Table.TableChild w18 = ((global::Gtk.Table.TableChild)(this.table1[this.label5]));
			w18.TopAttach = ((uint)(3));
			w18.BottomAttach = ((uint)(4));
			w18.XOptions = ((global::Gtk.AttachOptions)(4));
			w18.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label6 = new global::Gtk.Label();
			this.label6.Name = "label6";
			this.label6.Xalign = 0F;
			this.label6.LabelProp = global::Mono.Unix.Catalog.GetString("License Id");
			this.table1.Add(this.label6);
			global::Gtk.Table.TableChild w19 = ((global::Gtk.Table.TableChild)(this.table1[this.label6]));
			w19.TopAttach = ((uint)(5));
			w19.BottomAttach = ((uint)(6));
			w19.XOptions = ((global::Gtk.AttachOptions)(4));
			w19.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label7 = new global::Gtk.Label();
			this.label7.Name = "label7";
			this.label7.Xalign = 0F;
			this.label7.LabelProp = global::Mono.Unix.Catalog.GetString("License Type");
			this.table1.Add(this.label7);
			global::Gtk.Table.TableChild w20 = ((global::Gtk.Table.TableChild)(this.table1[this.label7]));
			w20.TopAttach = ((uint)(6));
			w20.BottomAttach = ((uint)(7));
			w20.XOptions = ((global::Gtk.AttachOptions)(4));
			w20.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label8 = new global::Gtk.Label();
			this.label8.Name = "label8";
			this.label8.Xalign = 0F;
			this.label8.LabelProp = global::Mono.Unix.Catalog.GetString("Email");
			this.table1.Add(this.label8);
			global::Gtk.Table.TableChild w21 = ((global::Gtk.Table.TableChild)(this.table1[this.label8]));
			w21.XOptions = ((global::Gtk.AttachOptions)(4));
			w21.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label9 = new global::Gtk.Label();
			this.label9.Name = "label9";
			this.label9.Xalign = 0F;
			this.label9.LabelProp = global::Mono.Unix.Catalog.GetString("Expires");
			this.table1.Add(this.label9);
			global::Gtk.Table.TableChild w22 = ((global::Gtk.Table.TableChild)(this.table1[this.label9]));
			w22.TopAttach = ((uint)(7));
			w22.BottomAttach = ((uint)(8));
			w22.XOptions = ((global::Gtk.AttachOptions)(4));
			w22.YOptions = ((global::Gtk.AttachOptions)(4));
			this.GtkAlignment2.Add(this.table1);
			this.frame2.Add(this.GtkAlignment2);
			this.GtkLabel3 = new global::Gtk.Label();
			this.GtkLabel3.Name = "GtkLabel3";
			this.GtkLabel3.LabelProp = global::Mono.Unix.Catalog.GetString("<b>License Info:</b>");
			this.GtkLabel3.UseMarkup = true;
			this.frame2.LabelWidget = this.GtkLabel3;
			this.vbox3.Add(this.frame2);
			global::Gtk.Box.BoxChild w25 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.frame2]));
			w25.Position = 2;
			w25.Expand = false;
			w25.Fill = false;
			w1.Add(this.vbox3);
			global::Gtk.Box.BoxChild w26 = ((global::Gtk.Box.BoxChild)(w1[this.vbox3]));
			w26.Position = 0;
			w26.Expand = false;
			w26.Fill = false;
			// Internal child Syntactik.MonoDevelop.License.LicenseInfo.ActionArea
			global::Gtk.HButtonBox w27 = this.ActionArea;
			w27.Name = "dialog1_ActionArea";
			w27.Spacing = 10;
			w27.BorderWidth = ((uint)(5));
			w27.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w28 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w27[this.buttonCancel]));
			w28.Expand = false;
			w28.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w29 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w27[this.buttonOk]));
			w29.Position = 1;
			w29.Expand = false;
			w29.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 569;
			this.DefaultHeight = 489;
			this.label4.MnemonicWidget = this.vbox3;
			this.Show();
		}
	}
}
