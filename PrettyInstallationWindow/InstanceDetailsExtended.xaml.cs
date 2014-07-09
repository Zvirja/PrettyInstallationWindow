#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SIM.Adapters.SqlServer;
using SIM.Adapters.WebServer;
using SIM.Base;
using SIM.Instances;
using SIM.Products;
using SIM.Tool.Base;
using SIM.Tool.Base.Pipelines;
using SIM.Tool.Base.Profiles;
using SIM.Tool.Base.Wizards;
using System.Collections.ObjectModel;
using System.Xml;

#endregion

namespace SIM.Tool.Windows.UserControls.Install
{
  #region



  #endregion

  /// <summary>
  ///   Interaction logic for CollectData.xaml
  /// </summary>
  [UsedImplicitly]
  public partial class InstanceDetailsExtended : IWizardStep, IFlowControl
  {
    #region Fields

    /// <summary>
    ///   The standalone products.
    /// </summary>
    private IEnumerable<Product> standaloneProducts;

    private readonly ICollection<string> allFrameworkVersions = Environment.Is64BitOperatingSystem ? new[] { "v2.0", "v2.0 32bit", "v4.0", "v4.0 32bit" } : new[] { "v2.0", "v4.0" };

    private InstallWizardArgs installParameters = null;

    #endregion

    #region Constructors

    /// <summary>
    ///   Initializes a new instance of the <see cref="InstanceDetails" /> class.
    /// </summary>
    public InstanceDetailsExtended()
    {
      this.InitializeComponent();

      this.netFramework.ItemsSource = allFrameworkVersions;
    }

    public static bool InstallEverywhere
    {
      get { return MainWindowHelper.Settings.AppInstallEverywhere.Value; }
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
      Assert.IsNotNull(name, @"Instance name isn't set");

      string host = this.hostName.Text.EmptyToNull();
      Assert.IsNotNull(host, "Hostname must not be emoty");

      string rootName = this.RootName.Text.EmptyToNull();
      Assert.IsNotNull(rootName, "Root folder name must not be emoty");

      string location = this.locationFolder.Text.EmptyToNull();
      Assert.IsNotNull(location, @"The location folder isn't set");

      string rootPath = Path.Combine(location, rootName);
      bool locationIsPhysical = FileSystem.Local.Directory.HasDriveLetter(rootPath);
      Assert.IsTrue(locationIsPhysical, "The location folder path must be physical i.e. contain a drive letter. Please choose another location folder");

      string webRootPath = Path.Combine(rootPath, "Website");

      bool websiteExists = WebServerManager.WebsiteExists(name);
      if (websiteExists)
      {
        using (var context = WebServerManager.CreateContext("InstanceDetails.OnMovingNext('{0}')".FormatWith(name)))
        {
          var site = context.Sites.Single(s => s.Name.EqualsIgnoreCase(name));
          var path = WebServerManager.GetWebRootPath(site);
          if (FileSystem.Local.Directory.Exists(path))
          {
            this.Alert("The website with the same name already exists, please choose another instance name.");
            return false;
          }

          if (
            WindowHelper.ShowMessage("There website with the same name already exists, but points to non-existing location. Would you like to delete it?",
              MessageBoxButton.OKCancel, MessageBoxImage.Asterisk) != MessageBoxResult.OK)
          {
            return false;
          }

          site.Delete();
          context.CommitChanges();
        }
      }
      websiteExists = WebServerManager.WebsiteExists(name);
      Assert.IsTrue(!websiteExists, "The website with the same name already exists, please choose another instance name.");

      bool hostExists = WebServerManager.HostBindingExists(host);
      Assert.IsTrue(!hostExists, "Website with the same host name already exists");

      bool rootFolderExists = FileSystem.Local.Directory.Exists(rootPath);
      if (rootFolderExists && InstanceManager.Instances != null)
      {
        if (InstanceManager.Instances.Any(i => i.WebRootPath.EqualsIgnoreCase(webRootPath)))
        {
          this.Alert("There is another instance with the same root path, please choose another folder");
          return false;
        }

        if (WindowHelper.ShowMessage("The folder with the same name already exists. Would you like to delete it?", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK) != MessageBoxResult.OK)
        {
          return false;
        }

        FileSystem.Local.Directory.DeleteIfExists(rootPath);
      }

      var connectionString = ProfileManager.GetConnectionString();
      SqlServerManager.Instance.ValidateConnectionString(connectionString);

      string licensePath = ProfileManager.Profile.License;
      Assert.IsNotNull(licensePath, @"The license file isn't set in the Settings window");
      FileSystem.Local.File.AssertExists(licensePath, "The {0} file is missing".FormatWith(licensePath));

      string framework = this.netFramework.SelectedValue.ToString();
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

      WindowHelper.ShowMessage(message.FormatWith(args), "Conflict is found", MessageBoxButton.OK, MessageBoxImage.Stop);
    }


    /// <summary>
    ///   Inits this instance.
    /// </summary>
    private void Init()
    {
      this.DataContext = new Model();
      this.standaloneProducts = ProductManager.StandaloneProducts;

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

        var frameworkVersions = new ObservableCollection<string>(this.allFrameworkVersions);

        var m = product.Manifest;
        if (m != null)
        {
          var node = (XmlElement)m.SelectSingleNode("/manifest/*/limitations");
          if (node != null)
          {
            foreach (XmlElement limitation in node.ChildNodes)
            {
              var lname = limitation.Name;
              switch (lname.ToLower())
              {
                case "framework":
                  {
                    var supportedVersions = limitation.SelectElements("supportedVersion");
                    if (supportedVersions != null)
                    {
                      ICollection<string> supportedVersionNames = supportedVersions.Select(supportedVersion => supportedVersion.InnerText).ToArray();
                      for (int i = frameworkVersions.Count - 1; i >= 0; i--)
                      {
                        if (!supportedVersionNames.Contains(frameworkVersions[i]))
                        {
                          frameworkVersions.RemoveAt(i);
                        }
                      }
                    }
                    break;
                  }
              }
            }
          }
        }

        this.netFramework.ItemsSource = frameworkVersions;
        this.netFramework.SelectedIndex = frameworkVersions.IndexOf(frameworkVersions.Last(framName => !framName.Contains("32")));
      }
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
        this.SelectLast(this.ProductRevision);
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

      if (string.IsNullOrEmpty(value))
      {
        SelectLast(element);
        return;
      }

      if (element.Items.Count > 0)
      {
        foreach (System.Linq.IGrouping<string, Product> item in element.Items)
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
    }

    private void SelectByValue([NotNull] Selector element, string value)
    {
      Assert.ArgumentNotNull(element, "element");

      if (string.IsNullOrEmpty(value))
      {
        SelectLast(element);
        return;
      }

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
          foreach (var item in element.Items)
          {
            if (item is ComboBoxItem)
            {
              if ((item as ComboBoxItem).Content.ToString().EqualsIgnoreCase(value, true))
              {
                element.SelectedItem = item;
                break;
              }
            }

            if (item is string)
            {
              if ((item as string).EqualsIgnoreCase(value, true))
              {
                element.SelectedItem = item;
                break;
              }
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
      if (this.installParameters.Product == null)
      {
        this.SelectProductByValue(ProductName, MainWindowHelper.Settings.AppInstallationDefaultProduct.Value);
        this.SelectProductByValue(ProductVersion, MainWindowHelper.Settings.AppInstallationDefaultProductVersion.Value);
        this.SelectByValue(ProductRevision, MainWindowHelper.Settings.AppInstallationDefaultProductRevision.Value);

        if (string.IsNullOrEmpty(MainWindowHelper.Settings.AppInstallationDefaultFramework.Value))
        {
          this.netFramework.SelectedIndex = 0;
        }
        else
        {
          this.SelectByValue(netFramework, MainWindowHelper.Settings.AppInstallationDefaultFramework.Value);
        }

        if (string.IsNullOrEmpty(MainWindowHelper.Settings.AppInstallationDefaultPoolMode.Value))
        {
          this.mode.SelectedIndex = 0;
        }
        else
        {
          this.SelectByValue(mode, MainWindowHelper.Settings.AppInstallationDefaultPoolMode.Value);
        }
      }
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

      var args = (InstallWizardArgs)wizardArgs;
      this.installParameters = args;

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
        var frameworkValue = info.FrameworkVersion + " " + (info.Enable32BitAppOnWin64 ? "32bit" : string.Empty);
        this.SelectByValue(this.netFramework, frameworkValue);
        this.SelectByValue(this.mode, info.ManagedPipelineMode ? "Integrated" : "Classic");
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
    }

    bool IWizardStep.SaveChanges(WizardArgs wizardArgs)
    {
      return true;
    }


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

    /*
     *This method should be put at the end of Init() method. 
     */
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
        pWindow.Height += 150;



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


    #endregion
  }
}