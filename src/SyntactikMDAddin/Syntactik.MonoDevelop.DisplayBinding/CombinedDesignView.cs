using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;

namespace Syntactik.MonoDevelop.DisplayBinding
{
    class CombinedDesignView : ViewContent
    {
        ViewContent content;
        Gtk.Widget control;
        private readonly List<TabView> _tabs = new List<TabView>();

        public CombinedDesignView(ViewContent content)
        {
            this.content = content;
            content.DirtyChanged += OnTextDirtyChanged;

            CommandRouterContainer crc = new CommandRouterContainer(content.Control, content, true);
            crc.Show();
            control = crc;

            IdeApp.Workbench.ActiveDocumentChanged += OnActiveDocumentChanged;
        }

        public override string TabPageLabel => GettextCatalog.GetString("Source");

        protected void AddButton(string label, Gtk.Widget page)
        {
            TabView view = new TabView(label, page);
            _tabs.Add(view);
            if (WorkbenchWindow != null)
            {
                view.WorkbenchWindow = WorkbenchWindow;
                WorkbenchWindow.AttachViewContent(view);
            }
        }

        public bool HasPage(Gtk.Widget page)
        {
            return _tabs.Any(p => p.Control.GetNativeWidget<Gtk.Widget>() == page);
        }

        public void RemoveButton(Gtk.Widget page)
        {
            /*			int i = notebook.PageNum (page);
                        if (i != -1)
                            RemoveButton (i);*/
        }

        public void RemoveButton(int npage)
        {
            /*			if (npage >= toolbar.Children.Length)
                            return;
                        notebook.RemovePage (npage);
                        Gtk.Widget cw = toolbar.Children [npage];
                        toolbar.Remove (cw);
                        cw.Destroy ();
                        ShowPage (0);*/
        }

        protected override void OnSetProject(global::MonoDevelop.Projects.Project project)
        {
            base.OnSetProject(project);
            content.Project = project;
        }

        public override ProjectReloadCapability ProjectReloadCapability => content.ProjectReloadCapability;

        protected override void OnWorkbenchWindowChanged()
        {
            base.OnWorkbenchWindowChanged();
            if (content != null)
                content.WorkbenchWindow = WorkbenchWindow;
            if (WorkbenchWindow != null)
            {
                foreach (TabView view in _tabs)
                {
                    view.WorkbenchWindow = WorkbenchWindow;
                    WorkbenchWindow.AttachViewContent(view);
                }
                WorkbenchWindow.ActiveViewContentChanged += OnActiveViewContentChanged;
            }
        }

        void OnActiveViewContentChanged(object o, ActiveViewContentEventArgs e)
        {
            if (WorkbenchWindow.ActiveViewContent == this)
                OnPageShown(0);
            else
            {
                TabView tab = WorkbenchWindow.ActiveViewContent as TabView;
                if (tab != null)
                {
                    int n = _tabs.IndexOf(tab);
                    if (n != -1)
                        OnPageShown(n + 1);
                }
            }
        }

        public void ShowPage(int npage)
        {
            if (WorkbenchWindow != null)
            {
                if (npage == 0)
                    WorkbenchWindow.SwitchView(0);
                else
                {
                    var view = _tabs[npage - 1];
                    WorkbenchWindow.SwitchView(view);
                }
            }
        }

        protected virtual void OnPageShown(int npage)
        {
        }

        public override void Dispose()
        {
            if (content == null)
                return;

            content.DirtyChanged -= OnTextDirtyChanged;
            IdeApp.Workbench.ActiveDocumentChanged -= OnActiveDocumentChanged;
            content.Dispose();

            content = null;
            control = null;

            base.Dispose();
        }

        public override Task Load(FileOpenInformation fileOpenInformation)
        {
            ContentName = fileOpenInformation.FileName;
            return content.Load(ContentName);
        }

        public override Control Control => control;

        public override Task Save(FileSaveInformation fileSaveInformation)
        {
            return content.Save(fileSaveInformation);
        }

        public override bool IsDirty
        {
            get
            {
                return content.IsDirty;
            }
            set
            {
                content.IsDirty = value;
            }
        }

        public override bool IsReadOnly => content.IsReadOnly;

        public virtual void AddCurrentWidgetToClass()
        {
        }

        void OnTextDirtyChanged(object s, EventArgs args)
        {
            OnDirtyChanged();
        }

        void OnActiveDocumentChanged(object s, EventArgs args)
        {
            if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.GetContent<CombinedDesignView>() == this)
                OnDocumentActivated();
        }

        protected virtual void OnDocumentActivated()
        {
        }

        protected override object OnGetContent(Type type)
        {
            return base.OnGetContent(type) ?? content?.GetContent(type);
        }

        public void JumpTo(int line, int column)
        {
            var ip = (TextEditor)content.GetContent(typeof(TextEditor));
            if (ip != null)
            {
                ShowPage(0);
                ip.SetCaretLocation(line, column);
            }
        }
    }


    class TabView : BaseViewContent
    {
        readonly Gtk.Widget _content;

        public TabView(string label, Gtk.Widget content)
        {
            TabPageLabel = label;
            _content = content;
        }

        protected override object OnGetContent(Type type)
        {
            if (type.IsInstanceOfType(_content))
                return _content;
            var delegator = _content as ICommandDelegatorRouter;
            if (WorkbenchWindow?.ActiveViewContent == this && delegator != null)
            {
                var target = delegator.GetDelegatedCommandTarget();
                if (type.IsInstanceOfType(target))
                    return target;
                var textEditor = target as TextEditor;
                var result = textEditor?.GetContents(type).FirstOrDefault();
                if (result != null)
                    return result;
                var viewContent = target as BaseViewContent;
                result = viewContent?.GetContent(type);
                if (result != null)
                    return result;
            }
            return base.OnGetContent(type);
        }

        public override Control Control => _content;

        public override string TabPageLabel { get; }
    }
}
