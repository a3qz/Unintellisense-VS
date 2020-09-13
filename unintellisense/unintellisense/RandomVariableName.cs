using System;
using System.ComponentModel.Design;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;
using unintellisense.Resources;
using System.Linq;

namespace unintellisense
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class RandomVariableName
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("e1a9cda8-096d-47d9-a217-61de9b23d13c");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private Random Random;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomVariableName"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private RandomVariableName(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            Random = new Random();

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static RandomVariableName Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in RandomVariableName's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new RandomVariableName(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IWpfTextView view = GetCurentTextView();
            if (view != null)
            {
                string text = GetText();

                if (!string.IsNullOrEmpty(text))
                    InsertText(view, text);
            }
        }

        private string GetText()
        {
            var adjective = GetAdjective();
            var noun = GetNoun();

            var result = adjective + noun.First().ToString().ToUpper() + noun.Substring(1);
            return result;
        }

        private static void InsertText(IWpfTextView view, string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                using (Microsoft.VisualStudio.Text.ITextEdit edit = view.TextBuffer.CreateEdit())
                {
                    if (!view.Selection.IsEmpty)
                    {
                        edit.Delete(view.Selection.SelectedSpans[0].Span);
                        view.Selection.Clear();
                    }

                    edit.Insert(view.Caret.Position.BufferPosition, text);
                    edit.Apply();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
            }
        }

        ///<summary>Gets the TextView for the active document.</summary>
        public static IWpfTextView GetCurentTextView()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IComponentModel componentModel = GetComponentModel();
            if (componentModel == null) return null;
            IVsEditorAdaptersFactoryService editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            IVsTextView nativeView = GetCurrentNativeTextView();

            if (nativeView != null)
                return editorAdapter.GetWpfTextView(nativeView);

            return null;
        }

        public static IVsTextView GetCurrentNativeTextView()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsTextManager textManager = (IVsTextManager)Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));
            Assumes.Present(textManager);

            textManager.GetActiveView(1, null, out IVsTextView activeView);
            return activeView;
        }

        public static IComponentModel GetComponentModel()
        {
            return (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
        }

        private string GetAdjective()
        {
            var idx = Random.Next(Words.Adjectives.Length);
            return Words.Adjectives[idx];
        }

        private string GetNoun()
        {
            var idx = Random.Next(Words.Nouns.Length);
            return Words.Nouns[idx];
        }
    }
}

