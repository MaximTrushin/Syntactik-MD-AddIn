﻿using System;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.Gui;

namespace Syntactik.MonoDevelop.DisplayBinding
{
    class HiddenWorkbenchWindow : IWorkbenchWindow
    {
        ExtensionContext _extensionContext;
        private readonly FileTypeCondition _fileTypeCondition = new FileTypeCondition();

        public string Title { get; set; }

        public string DocumentType
        {
            get { return ""; }
            set { }
        }

        public Document Document
        {
            get;
            set;
        }

        public bool ShowNotification
        {
            get { return false; }
            set { }
        }

        public ExtensionContext ExtensionContext => _extensionContext;

        public ViewContent ViewContent { get; set; }

        public IEnumerable<BaseViewContent> SubViewContents => new BaseViewContent[0];

        public BaseViewContent ActiveViewContent
        {
            get { return ViewContent; }
            set { }
        }

        public bool CloseWindow(bool force)
        {
            return true;
        }

        public void SelectWindow()
        {
        }

        public void SwitchView(int viewNumber)
        {
        }
        public void SwitchView(BaseViewContent subViewContent)
        {
        }

        public int FindView<T>()
        {
            return -1;
        }

        public void AttachViewContent(BaseViewContent content)
        {
            ViewContent = (ViewContent) content;
            content.WorkbenchWindow = this;
            _fileTypeCondition.SetFileName(ViewContent.ContentName ?? ViewContent.UntitledName);
            _extensionContext = AddinManager.CreateExtensionContext();
            _extensionContext.RegisterCondition("FileType", _fileTypeCondition);
        }

        public void InsertViewContent(int index, BaseViewContent subViewContent)
        {
        }

        public DocumentToolbar GetToolbar(BaseViewContent targetView)
        {
            return null;
        }

        public event EventHandler DocumentChanged { add { } remove { } }
        public event WorkbenchWindowEventHandler Closing { add { } remove { } }
        public event WorkbenchWindowEventHandler Closed { add { } remove { } }
        public event ActiveViewContentEventHandler ActiveViewContentChanged { add { } remove { } }
        public event EventHandler ViewsChanged { add { } remove { } }
    }
}
