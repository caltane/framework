using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Signum.Entities;
using System.Windows.Threading;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using System.Linq;
using System.Collections.Generic;
using Signum.Entities.Reflection;
using System.Windows.Input;
using System.Windows.Automation.Peers;

namespace Signum.Windows
{
    public partial class NormalWindow
    {
        public static readonly DependencyProperty MainControlProperty =
            DependencyProperty.Register("MainControl", typeof(Control), typeof(NormalWindow));
        public Control MainControl
        {
            get { return (Control)GetValue(MainControlProperty); }
            set { SetValue(MainControlProperty, value); }
        }

        public static readonly DependencyProperty AllowErrorsProperty =
            DependencyProperty.Register("AllowErrors", typeof(AllowErrors), typeof(NormalWindow), new UIPropertyMetadata(AllowErrors.Ask));
        public AllowErrors AllowErrors
        {
            get { return (AllowErrors)GetValue(AllowErrorsProperty); }
            set { SetValue(AllowErrorsProperty, value); }
        }

        public static readonly DependencyProperty SaveProtectedProperty =
            DependencyProperty.Register("SaveProtected", typeof(bool), typeof(NormalWindow), new UIPropertyMetadata(false));
        public bool SaveProtected
        {
            get { return (bool)GetValue(SaveProtectedProperty); }
            set { SetValue(SaveProtectedProperty, value); }
        }

        public static readonly DependencyProperty ShowOperationsProperty =
            DependencyProperty.Register("ShowOperations", typeof(bool), typeof(NormalWindow), new PropertyMetadata(true));
        public bool ShowOperations
        {
            get { return (bool)GetValue(ShowOperationsProperty); }
            set { SetValue(ShowOperationsProperty, value); }
        }

        public static readonly DependencyProperty ViewModeProperty =
            DependencyProperty.Register("ViewMode", typeof(ViewMode), typeof(NormalWindow), new PropertyMetadata(ViewMode.Navigate));
        public ViewMode ViewMode
        {
            get { return (ViewMode)GetValue(ViewModeProperty); }
            set { SetValue(ViewModeProperty, value); }
        }

        public static readonly DependencyProperty AvoidShowCloseDialogProperty =
            DependencyProperty.Register("AvoidShowCloseDialog", typeof(bool), typeof(NormalWindow), new PropertyMetadata(false));
        public bool AvoidShowCloseDialog
        {
            get { return (bool)GetValue(AvoidShowCloseDialogProperty); }
            set { SetValue(AvoidShowCloseDialogProperty, value); }
        }

        public ButtonBar ButtonBar
        {
            get { return this.buttonBar; }
        }

        public NormalWindow()
        {
            this.InitializeComponent();

            this.Loaded += NormalWindow_Loaded;
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(NormalWindow_DataContextChanged);

            Common.AddChangeDataContextHandler(this, ChangeDataContext_Handler);

            RefreshEnabled();
        }

        void NormalWindow_Loaded(object sender, EventArgs e)
        {
            ButtonBar.OkVisible = ViewMode == Windows.ViewMode.View;
        }

        void ChangeDataContext_Handler(object sender, ChangeDataContextEventArgs e)
        {
            if (e.Refresh)
            {
                var l = ((IdentifiableEntity)this.DataContext).ToLite().Retrieve();
                this.DataContext = null;
                this.DataContext = l;
                e.Handled = true;
            }
            else
            {
                if (e.NewDataContext == null)
                    throw new ArgumentNullException("NewDataContext");

                Type type = Common.GetPropertyRoute(MainControl).Type;
                Type entityType = e.NewDataContext.GetType();
                if (type != null && !type.IsAssignableFrom(entityType))
                    throw new InvalidCastException("The DataContext is a {0} but TypeContext is {1}".Formato(entityType.Name, type.Name));

                DataContext = null;
                DataContext = e.NewDataContext;
                e.Handled = true;
            }
        }

        void NormalWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            RefreshEnabled();

            var entity = e.NewValue;
            if(entity == null)
                return;

            ButtonBarEventArgs ctx = new ButtonBarEventArgs
            {
                MainControl = MainControl,
                ViewButtons = ViewMode,
                SaveProtected = SaveProtected,
                ShowOperations = ShowOperations,
            };

            List<FrameworkElement> elements = Navigator.Manager.GetToolbarButtons(entity, ctx);

            ButtonBar.SetButtons(elements);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is IdentifiableEntity && SaveProtected)
            {
                if (!this.HasChanges())
                    DialogResult = true;
                else
                {
                    var result = MessageBox.Show(
                        NormalWindowMessage.ThereAreChangesContinue.NiceToString(),
                        NormalWindowMessage.ThereAreChanges.NiceToString(),
                        MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK);

                    if (result == MessageBoxResult.Cancel)
                        return;

                    DialogResult = false;

                }
            }
            else
            {
                string errors = this.GetErrors();

                if (errors.HasText())
                {
                    Type type = DataContext.GetType();

                    switch (AllowErrors)
                    {
                        case AllowErrors.Yes: break;
                        case AllowErrors.No:
                            MessageBox.Show(this,
                                NormalWindowMessage.The0HasErrors1.NiceToString().ForGenderAndNumber(type.GetGender()).Formato(type.NiceName(), errors.Indent(3)),
                                NormalWindowMessage.FixErrors.NiceToString(),
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);
                            return;
                        case AllowErrors.Ask:
                            if (MessageBox.Show(this,
                                NormalWindowMessage.The0HasErrors1.NiceToString().ForGenderAndNumber(type.GetGender()).Formato(type.NiceName(), errors.Indent(3)) + "\r\n" + NormalWindowMessage.ContinueAnyway.NiceToString(),
                                NormalWindowMessage.ContinueWithErrors.NiceToString(),
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Exclamation,
                                MessageBoxResult.None) == MessageBoxResult.No)
                                return;
                            break;
                    }
                }

                base.DialogResult = true;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            MoveFocus();

            if (this.DataContext != null && this.HasChanges() && !AvoidShowCloseDialog)
            {
                if (buttonBar.ViewMode == ViewMode.Navigate)
                {
                    var result = MessageBox.Show(
                      NormalWindowMessage.ThereAreChangesContinue.NiceToString(),
                      NormalWindowMessage.ThereAreChanges.NiceToString(),
                      MessageBoxButton.OKCancel,
                      MessageBoxImage.Question,
                      MessageBoxResult.OK);

                    if (result == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }

                }
                else
                {
                    if (DialogResult == null)
                    {
                        var result = MessageBox.Show(this,
                            NormalWindowMessage.LoseChanges.NiceToString(),
                            NormalWindowMessage.ThereAreChanges.NiceToString(),
                            MessageBoxButton.OKCancel,
                            MessageBoxImage.Question,
                            MessageBoxResult.OK);

                        if (result == MessageBoxResult.Cancel)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
            }
        }

        private static void MoveFocus()
        {
            // Change keyboard focus.
            UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;

            if (elementWithFocus != null)
            {
                elementWithFocus.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        void RefreshEnabled()
        {
            buttonBar.ReloadButton.IsEnabled = (DataContext as IdentifiableEntity).Try(ei => !ei.IsNew) ?? false;
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            if (this.LooseChangesIfAny())
            {
                IdentifiableEntity ei = (IdentifiableEntity)DataContext;
                DataContext = null;  // Equal returns true 
                DataContext = Server.Retrieve(ei.GetType(), ei.Id);
            }
        }

        private void widgetPanel_ExpandedCollapsed(object sender, RoutedEventArgs e)
        {
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }

        public void SetTitleText(string text)
        {
            this.entityTitle.SetTitleText(text);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new NormalWindowAutomationPeer(this);
        }

    }

    public enum AllowErrors
    {
        Ask,
        Yes,
        No,
    }

    public interface IHaveToolBarElements
    {
        List<FrameworkElement> GetToolBarElements(object dataContext, ButtonBarEventArgs ctx);
    }

    public class ButtonBarEventArgs
    {
        public Control MainControl { get; set; }
        public ViewMode ViewButtons { get; set; }
        public bool SaveProtected { get; set; }
        public bool ShowOperations { get; set; }
    }

    public class NormalWindowAutomationPeer : WindowAutomationPeer
    {
        public NormalWindowAutomationPeer(NormalWindow normalWindow)
            : base(normalWindow)
        {
        }

        protected override string GetClassNameCore()
        {
            return "NormalWindow";
        }
    }

}
