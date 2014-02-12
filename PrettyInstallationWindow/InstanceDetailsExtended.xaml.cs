#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SIM.Adapters.SqlServer;
using SIM.Adapters.WebServer;
using SIM.Base;
using SIM.Instances;
using SIM.Products;
using SIM.Tool.Base;
using SIM.Tool.Base.Pipelines;
using SIM.Tool.Base.Profiles;
using SIM.Tool.Base.Wizards;

#endregion

namespace SIM.Tool.UserControls.Install
{
  using System.Windows.Controls.Primitives;
  using System.Windows.Media;

  /// <summary>
  /// Interaction logic for InstanceDetailsExtended.xaml
  /// </summary>
  public partial class InstanceDetailsExtended : IWizardStep, IFlowControl
  {
    #region Fields

    /// <summary>
    ///   The standalone products.
    /// </summary>
    private IEnumerable<Product> standaloneProducts;

    #endregion

    #region Constructors

    /// <summary>
    ///   Initializes a new instance of the <see cref="InstanceDetails" /> class.
    /// </summary>
    public InstanceDetailsExtended()
    {
      this.InitializeComponent();
    }

    public static bool InstallEverywhere
    {
      get { return AdvancedSettings.CoreInstallEverywhere.Value; }
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///   Call base.OnMovingNext() in the end of the method if everything is OK
    /// </summary>
    /// <returns> The on moving next. </returns>
    public bool OnMovingNext(WizardArgs wizardArgs)
    {
      Product product = this.ProductRevision.SelectedValue as Product;
      Assert.IsNotNull(product, "product");

      string name = this.InstanceName.Text.EmptyToNull();
      Assert.IsNotNull(name, "Instance name isn't set");

      string host = this.hostName.Text.EmptyToNull();
      Assert.IsNotNull(host, "Hostname must not be emoty");

      string rootName = this.RootName.Text.EmptyToNull();
      Assert.IsNotNull(rootName, "Root folder name must not be emoty");

      string location = this.locationFolder.Text.EmptyToNull();
      Assert.IsNotNull(location, "The location folder isn't set");

      string rootPath = Path.Combine(location, rootName);
      bool locationIsPhysical = FileSystem.Local.HasDriveLetter(rootPath);
      Assert.IsTrue(locationIsPhysical, "The location folder path must be physical i.e. contain a drive letter. Please choose another location folder");

      string webRootPath = Path.Combine(rootPath, "Website");

      bool websiteExists = WebServerManager.WebsiteExists(name);
      Assert.IsTrue(!websiteExists, "The website with the same name already exists, please choose another instance name.");

      bool hostExists = WebServerManager.HostBindingExists(host);
      Assert.IsTrue(!hostExists, "Website with the same host name already exists");

      bool rootFolderExists = FileSystem.Local.DirectoryExists(rootPath);
      if (rootFolderExists && InstanceManager.Instances != null)
      {
        if (InstanceManager.Instances.Any(i => i.WebRootPath.EqualsIgnoreCase(webRootPath)))
        {
          this.Alert("There is another instance with the same root path, please choose another folder");
          return false;
        }

        if (MessageBox.Show("The folder with the same name already exists. Would you like to delete it?", "SIM", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK) != MessageBoxResult.OK)
        {
          return false;
        }

        FileSystem.Local.DeleteIfExists(rootPath);
      }

      var connectionString = ProfileManager.GetConnectionString();
      SqlServerManager.Instance.ValidateConnectionString(connectionString);

      string licensePath = ProfileManager.Profile.License;
      Assert.IsNotNull(licensePath, "The license file isn't set in the Settings window");
      FileSystem.Local.AssertFileExists(licensePath, "The {0} file is missing".FormatWith(licensePath));

      string framework = (string)((ListBoxItem)this.netFramework.SelectedValue).Content;
      string[] frameworkArr = framework.Split(' ');
      Assert.IsTrue(frameworkArr.Length > 0, "impossible");
      bool force32Bit = frameworkArr.Length == 2;
      bool isClassic = ((string)((ListBoxItem)this.mode.SelectedValue).Content).EqualsIgnoreCase("Classic");

      var args = (InstallWizardArgs)wizardArgs;
      args.InstanceName = name;
      args.InstanceHost = host;
      args.InstanceWebRootPath = webRootPath;
      args.InstanceRootName = rootName;
      args.InstanceRootPath = rootPath;
      args.InstanceProduct = product;
      args.InstanceConnectionString = connectionString;
      args.LicenseFileInfo = new FileInfo(licensePath);
      args.InstanceAppPoolInfo = new AppPoolInfo { FrameworkVersion = frameworkArr[0].EmptyToNull() ?? "v2.0", Enable32BitAppOnWin64 = force32Bit, ManagedPipelineMode = !isClassic };
      args.Product = product;

      return true;
    }

    public bool OnMovingBack(WizardArgs wizardArg)
    {
      return true;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Alerts the specified message.
    /// </summary>
    /// <param name="message">
    /// The message. 
    /// </param>
    /// <param name="args">
    /// The arguments. 
    /// </param>
    protected void Alert([NotNull] string message, [NotNull] params object[] args)
    {
      Assert.ArgumentNotNull(message, "message");
      Assert.ArgumentNotNull(args, "args");

      MessageBox.Show(message.FormatWith(args), "Conflict is found", MessageBoxButton.OK, MessageBoxImage.Stop);
    }


    /// <summary>
    ///   Inits this instance.
    /// </summary>
    private void Init()
    {
      this.DataContext = new Model();
      this.standaloneProducts = ProductManager.StandaloneProducts;
      if (!Environment.Is64BitOperatingSystem)
      {
        foreach (var comboBoxItem in this.netFramework.Items.OfType<ComboBoxItem>().Where(cb => ((string)cb.Content).Contains("32bit")).ToArray())
        {
          this.netFramework.Items.Remove(comboBoxItem);
        }
      }

      this.ModifyParentSize();
    }

    /// <summary>
    /// Instances the name text changed.
    /// </summary>
    /// <param name="sender">
    /// The sender. 
    /// </param>
    /// <param name="e">
    /// The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data. 
    /// </param>
    private void InstanceNameTextChanged([CanBeNull] object sender, [CanBeNull] TextChangedEventArgs e)
    {
      string name = this.InstanceName.Text;
      this.hostName.Text = name;
      this.RootName.Text = name;
      this.sqlPrefix.Text = name;
    }

    /// <summary>
    /// Picks the location folder.
    /// </summary>
    /// <param name="sender">
    /// The sender. 
    /// </param>
    /// <param name="e">
    /// The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data. 
    /// </param>
    private void PickLocationFolder([CanBeNull] object sender, [CanBeNull] RoutedEventArgs e)
    {
      WindowHelper.PickFolder("Choose location folder", this.locationFolder, null);
    }

    /// <summary>
    /// Products the name changed.
    /// </summary>
    /// <param name="sender">
    /// The sender. 
    /// </param>
    /// <param name="e">
    /// The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data. 
    /// </param>
    private void ProductNameChanged([CanBeNull] object sender, [CanBeNull] SelectionChangedEventArgs e)
    {
      IGrouping<string, Product> grouping = this.ProductName.SelectedValue as IGrouping<string, Product>;

      if (grouping != null)
      {
        this.ProductVersion.DataContext = grouping.GroupBy(p => p.ShortVersion).OrderBy(p => p.Key);
        this.SelectFirst(this.ProductVersion);
      }
    }

    /// <summary>
    /// Products the revision changed.
    /// </summary>
    /// <param name="sender">
    /// The sender. 
    /// </param>
    /// <param name="e">
    /// The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data. 
    /// </param>
    private void ProductRevisionChanged([CanBeNull] object sender, [CanBeNull] SelectionChangedEventArgs e)
    {
      Product product = this.ProductRevision.SelectedValue as Product;
      if (product != null)
      {
        string name = product.DefaultInstanceName;
        this.InstanceName.Text = name;
        this.hostName.Text = name;
        this.RootName.Text = product.DefaultFolderName;
        this.sqlPrefix.Text = name; 

        /*var m = product.Manifest;
        if(m != null)
        {
          var node = (XmlElement) m.SelectSingleNode(ManifestPrefix + "standalone/install/limitations");
          if(node != null)
          {
            foreach (var limitation in node.ChildElements())
            {
              var lname = limitation.Name;
              switch (lname.ToLower())
              {
                case "apppoolinfo":
                {
                  foreach (var nestedlimitation in limitation.ChildElements())
                  {
                    v
                  }
                  
                  break;
                }
              }
            }
          }
        }*/
      }
/*      FocusManager.SetFocusedElement(this, InstanceName);
      Keyboard.Focus(InstanceName);
      InstanceName.SelectionStart = InstanceName.Text.Length;*/
    }

    /// <summary>
    /// Products the version changed.
    /// </summary>
    /// <param name="sender">
    /// The sender. 
    /// </param>
    /// <param name="e">
    /// The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data. 
    /// </param>
    private void ProductVersionChanged([CanBeNull] object sender, [CanBeNull] SelectionChangedEventArgs e)
    {
      IGrouping<string, Product> grouping = this.ProductVersion.SelectedValue as IGrouping<string, Product>;
      if (grouping != null)
      {
        this.ProductRevision.DataContext = grouping.OrderBy(p => p.Revision);
        this.SelectFirst(this.ProductRevision);
      }
    }

    /// <summary>
    /// Selects the specified element.
    /// </summary>
    /// <param name="element">
    /// The element. 
    /// </param>
    /// <param name="value">
    /// The value. 
    /// </param>
    private void Select([NotNull] Selector element, [NotNull] string value)
    {
      Assert.ArgumentNotNull(element, "element");
      Assert.ArgumentNotNull(value, "value");

      if (element.Items.Count > 0)
      {
        for (int i = 0; i < element.Items.Count; ++i)
        {
          object item0 = element.Items[i];
          IGrouping<string, Product> item1 = item0 as IGrouping<string, Product>;
          if (item1 != null)
          {
            string key = item1.Key;
            if (key.EqualsIgnoreCase(value))
            {
              element.SelectedIndex = i;
              break;
            }
          }
          else
          {
            Product item2 = item0 as Product;
            if (item2 != null)
            {
              string key = item2.Revision;
              if (key.EqualsIgnoreCase(value))
              {
                element.SelectedIndex = i;
                break;
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Selects the first.
    /// </summary>
    /// <param name="element">
    /// The element. 
    /// </param>
    private void SelectFirst([NotNull] Selector element)
    {
      Assert.ArgumentNotNull(element, "element");

      if (element.Items.Count > 0)
      {
        element.SelectedIndex = 0;
      }
    }

    /// <summary>
    /// Selects last.
    /// </summary>
    /// <param name="element">
    /// The element. 
    /// </param>
    private void SelectLast([NotNull] Selector element)
    {
      Assert.ArgumentNotNull(element, "element");

      if (element.Items.Count > 0)
      {
        element.SelectedIndex = element.Items.Count - 1;
      }
    }

    /// <summary>
    /// Selects by value.
    /// </summary>
    /// <param name="element">
    /// The element. 
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    private void SelectProductByValue([NotNull] Selector element, string value)
    {
      Assert.ArgumentNotNull(element, "element");

      if (element.Items.Count > 0)
      {
        foreach (IGrouping<string,Product> item in element.Items)
        {
          if (item.First().Name.EqualsIgnoreCase(value, true))
          {
            element.SelectedItem = item;
            break;
          }

          if (item.First().Version.EqualsIgnoreCase(value, true))
          {
            element.SelectedItem = item;
            break;
          }
        }
      }
      var listBox = element as ListBox;
      if (listBox != null)
      {
        if(element.Items.Count > 0)
          listBox.ScrollIntoView(element.Items[element.Items.Count - 1]);
        listBox.ScrollIntoView(element.SelectedItem);
      }
    }

    private void SelectByValue([NotNull] Selector element, string value)
    {
      Assert.ArgumentNotNull(element, "element");

      if (element.Items.Count > 0)
      {
        if (element.Items[0].GetType() == typeof(Product))
        {
          foreach (Product item in element.Items)
          {
            if (item.Name.EqualsIgnoreCase(value, true))
            {
              element.SelectedItem = item;
              break;
            }
            if (item.Revision.EqualsIgnoreCase(value, true))
            {
              element.SelectedItem = item;
              break;
            }
          }
        }
        else
        {
          foreach (ContentControl item in element.Items)
          {
            if (item.Content.ToString().EqualsIgnoreCase(value, true))
            {
              element.SelectedItem = item;
              break;
            }
          }
        }
      }
    }

    /// <summary>
    /// Windows the loaded.
    /// </summary>
    /// <param name="sender">
    /// The sender. 
    /// </param>
    /// <param name="e">
    /// The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data. 
    /// </param>
    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
      //init

      this.SelectProductByValue(ProductName, AdvancedSettings.AppInstallationDefaultProduct.Value);
      this.SelectProductByValue(ProductVersion, AdvancedSettings.AppInstallationDefaultProductVersion.Value);
      this.SelectByValue(ProductRevision, AdvancedSettings.AppInstallationDefaultProductRevision.Value);
      this.SelectByValue(netFramework, AdvancedSettings.AppInstallationDefaultFramework.Value);
      this.SelectByValue(mode, AdvancedSettings.AppInstallationDefaultPoolMode.Value);

      /*FocusManager.SetFocusedElement(ProductRevision.Parent, ProductRevision);
      Keyboard.Focus(ProductRevision);  
*/
      //netFramework
    }

    #endregion

    #region Nested type: Model

    /// <summary>
    ///   Defines the model class.
    /// </summary>
    public class Model
    {
      #region Fields

      /// <summary>
      ///   The products.
      /// </summary>
      [CanBeNull]
      [UsedImplicitly]
      public readonly Product[] Products = ProductManager.StandaloneProducts.ToArray();

      /// <summary>
      ///   The name.
      /// </summary>
      [NotNull]
      private string name;

      #endregion

      #region Properties

      /// <summary>
      ///   Gets or sets the name.
      /// </summary>
      /// <value> The name. </value>
      [NotNull]
      [UsedImplicitly]
      public string Name
      {
        get
        {
          return this.name;
        }

        set
        {
          Assert.IsNotNull(value.EmptyToNull(), "Name must not be empty");
          this.name = value;
        }
      }

      /// <summary>
      ///   Gets or sets SelectedProductGroup1.
      /// </summary>
      [UsedImplicitly]
      public IGrouping<string, Product> SelectedProductGroup1 { get; set; }

      #endregion
    }

    #endregion

    #region IWizardStep Members

    void IWizardStep.InitializeStep(WizardArgs wizardArgs)
    {
      this.Init();

      this.locationFolder.Text = ProfileManager.Profile.InstancesFolder;
      this.ProductName.DataContext = this.standaloneProducts.GroupBy(p => p.Name);
      this.netFramework.SelectedIndex = 0;

      var args = (InstallWizardArgs)wizardArgs;

      Product product = args.Product;
      if (product != null)
      {
        this.Select(this.ProductName, product.Name);
        this.Select(this.ProductVersion, product.ShortVersion);
        this.Select(this.ProductRevision, product.Revision);
      }
      else
      {
        this.SelectFirst(this.ProductName);
      }

      AppPoolInfo info = args.InstanceAppPoolInfo;
      if (info != null)
      {
        if (info.FrameworkVersion == "v4.0")
        {
          this.netFramework.SelectedIndex = 2;
        }

        if (info.Enable32BitAppOnWin64)
        {
          this.netFramework.SelectedIndex++;
        }

        if (!info.ManagedPipelineMode)
        {
          this.mode.SelectedIndex = 1;
        }
      }

      string name = args.InstanceName;
      if (!string.IsNullOrEmpty(name))
      {
        this.InstanceName.Text = name;
      }

      string rootName = args.InstanceRootName;
      if (!string.IsNullOrEmpty(rootName))
      {
        this.RootName.Text = rootName;
      }

      string host = args.InstanceHost;

      if (!string.IsNullOrEmpty(host))
      {
        this.hostName.Text = host;
      }

      if (rootName != null)
      {
        string location = args.InstanceRootPath.TrimEnd(rootName).Trim(new[] { '/', '\\' });
        if (!string.IsNullOrEmpty(location))
        {
          this.locationFolder.Text = location;
        }
      }
      //
      this.WindowLoaded(null, null);
    }

    bool IWizardStep.SaveChanges(WizardArgs wizardArgs)
    {
      return true;
    }

    #endregion

    private double originalHeight;
    private double originalWidth;

    private void InstanceName_OnLostFocus(object sender, RoutedEventArgs e)
    {
      var textBox = sender as TextBox;
      var restricted = new[] { " ", "." };
      var newTextValue = textBox.Text;
      foreach (var restchar in restricted)
        newTextValue = newTextValue.Replace(restchar, string.Empty);
      if (!newTextValue.Equals(textBox.Text, StringComparison.OrdinalIgnoreCase))
        textBox.Text = newTextValue;
    }

    private void ModifyParentSize()
    {
      var parent = this.Parent;
      while (!(parent is Window))
      {
        parent = VisualTreeHelper.GetParent(parent);
      }
      var pWindow = parent as Window;
      if (originalHeight == 0)
        originalHeight = pWindow.Height;
      /*originalWidth = pWindow.Width;//*/
      if (pWindow.Height == originalHeight)
        pWindow.Height += 110;
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
      var expander = sender as Expander;

      var parent = this.Parent;
      while (!(parent is Window))
      {
        parent = VisualTreeHelper.GetParent(parent);
      }
      var pWindow = parent as Window;
      pWindow.Height += 56;
    }

    private void Expander_Collapsed(object sender, RoutedEventArgs e)
    {
      var parent = this.Parent;
      while (!(parent is Window))
      {
        parent = VisualTreeHelper.GetParent(parent);
      }
      var pWindow = parent as Window;
      pWindow.Height -= 56;
    }


  }
}
