
// This file has been generated by the GUI designer. Do not modify.

using MonoDevelop.Core;

namespace Syntactik.MonoDevelop
{
	public partial class CompilerOptionsWidget
	{
		private global::Gtk.VBox vbox2;
		private global::Gtk.Label label1;
		private global::MonoDevelop.Components.FolderEntry folderEntry;

		protected void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget MalinaBinding.CompilerOptionsWidget
			global::Stetic.BinContainer.Attach (this);
			this.Name = "MalinaBinding.CompilerOptionsWidget";
			// Container child MalinaBinding.CompilerOptionsWidget.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox ();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label ();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = GettextCatalog.GetString ("Output directory:");
			this.vbox2.Add (this.label1);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox2 [this.label1]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
            this.folderEntry = new global::MonoDevelop.Components.FolderEntry();
            this.folderEntry.Name = "folderEntry";
			this.vbox2.Add (this.folderEntry);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2 [this.folderEntry]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
			this.Add (this.vbox2);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.Hide ();
		}
	}
}
