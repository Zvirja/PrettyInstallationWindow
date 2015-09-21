#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Sitecore.Diagnostics;
using Sitecore.Diagnostics.Annotations;

#endregion

namespace PrettyInstallationWindow
{
  internal class InstanceDetailsDialogExtensions
  {
    #region Fields

    private const int ExpanderHeightDelta = 56;

    #endregion

    #region Constructors

    public InstanceDetailsDialogExtensions(System.Windows.Controls.UserControl dialogControl, ListBox productVersionControl, ListBox productRevisionControl, Expander moreSettinsExpander, TextBox instanceName)
    {
      this.DialogControl = dialogControl;
      this.ParentWindow = FindParentWindow(this.DialogControl.Parent);

      this.ProductVersion = productVersionControl;
      this.ProductRevision = productRevisionControl;
      this.MoreSettingsExpander = moreSettinsExpander;
      this.InstanceName = instanceName;
    }

    #endregion

    #region Properties

    public TextBox InstanceName { get; set; }
    public Expander MoreSettingsExpander { get; set; }
    public ListBox ProductRevision { get; set; }

    public ListBox ProductVersion { get; set; }

    private System.Windows.Controls.UserControl DialogControl { get; set; }
    private double OriginalHeight { get; set; }

    private Window ParentWindow { get; set; }

    #endregion

    #region Methods

    public void InitializeCustomExtensions()
    {
      this.ModifyParentSize();

      /* Scroll to selected element in ListBox control */
      this.ProductVersion.SelectionChanged += this.ListBoxOnSelectionChangedScrollTo;
      this.ProductRevision.SelectionChanged += this.ListBoxOnSelectionChangedScrollTo;

      this.ListBoxOnSelectionChangedScrollTo(this.ProductVersion, null);
      this.ListBoxOnSelectionChangedScrollTo(this.ProductRevision, null);

      /*Fix window height on expander expanding/collapsing*/
      this.MoreSettingsExpander.Expanded += (sender, args) => this.ParentWindow.Height += ExpanderHeightDelta;
      this.MoreSettingsExpander.Collapsed += (sender, args) => this.ParentWindow.Height -= ExpanderHeightDelta;

      this.InstanceName.LostFocus += this.InstanceName_OnLostFocus;
    }

    private static Window FindParentWindow(DependencyObject initialControl)
    {
      var parent = initialControl;
      while (!(parent is Window))
      {
        parent = VisualTreeHelper.GetParent(parent);
      }

      var pWindow = parent as Window;

      Assert.IsNotNull(parent, "Cannot find parent window control");

      return pWindow;
    }

    private void InstanceName_OnLostFocus(object sender, RoutedEventArgs e)
    {
      var textBox = sender as TextBox;
      var restricted = new[]
      {
        " ", "."
      };
      var newTextValue = textBox.Text;
      foreach (var restchar in restricted)
      {
        newTextValue = newTextValue.Replace(restchar, string.Empty);
      }
      if (!newTextValue.Equals(textBox.Text, StringComparison.OrdinalIgnoreCase))
      {
        textBox.Text = newTextValue;
      }
    }

    private void ListBoxOnSelectionChangedScrollTo(object sender, [CanBeNull] SelectionChangedEventArgs selectionChangedEventArgs)
    {
      var listBox = (ListBox)sender;
      var selectedItem = listBox.SelectedItem;
      if (selectedItem != null)
      {
        listBox.ScrollIntoView(selectedItem);
      }
    }


    private void ModifyParentSize()
    {
      if (this.OriginalHeight == 0)
      {
        this.OriginalHeight = this.ParentWindow.Height;
      }

      if (this.ParentWindow.Height == OriginalHeight)
      {
        this.ParentWindow.Height += PluginProperties.DialogHeightDelta.Value;
      }
    }

    #endregion
  }
}