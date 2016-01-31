using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WAbout.xaml
  /// </summary>
  public partial class WAbout {
    public AssemblyInfo EntryAssemblyInfo;

    public WAbout() {
      InitializeComponent();
      EntryAssemblyInfo = new AssemblyInfo(Assembly.GetEntryAssembly());
      DataContext = EntryAssemblyInfo;
    }

    private void BtnDonate_OnClick(object sender, RoutedEventArgs e) {
      System.Diagnostics.Process.Start(@"https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=9FDUA6VBNWMB2&lc=CZ&item_name=Martin%20Holy&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted");
    }

    private void Homepage_OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
      System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
      e.Handled = true;
    }
  }

  public class AssemblyInfo {
    public AssemblyInfo(Assembly assembly) {
      if (assembly == null)
        throw new ArgumentNullException(nameof(assembly));
      _assembly = assembly;
    }

    private readonly Assembly _assembly;

    /// <summary>
    /// Gets the title property
    /// </summary>
    public string ProductTitle {
      get {
        return GetAttributeValue<AssemblyTitleAttribute>(a => a.Title,
          Path.GetFileNameWithoutExtension(_assembly.CodeBase));
      }
    }

    /// <summary>
    /// Gets the application's version
    /// </summary>
    public string Version => _assembly.GetName().Version?.ToString() ?? string.Empty;

    /// <summary>
    /// Gets the application's file version
    /// </summary>
    public string FileVersion {
      get { return GetAttributeValue<AssemblyFileVersionAttribute>(a => a.Version); }
    }

  /// <summary>
    /// Gets the description about the application.
    /// </summary>
    public string Description {
      get { return GetAttributeValue<AssemblyDescriptionAttribute>(a => a.Description); }
    }

    /// <summary>
    ///  Gets the product's full name.
    /// </summary>
    public string Product {
      get { return GetAttributeValue<AssemblyProductAttribute>(a => a.Product); }
    }

    /// <summary>
    /// Gets the copyright information for the product.
    /// </summary>
    public string Copyright {
      get { return GetAttributeValue<AssemblyCopyrightAttribute>(a => a.Copyright); }
    }

    /// <summary>
    /// Gets the company information for the product.
    /// </summary>
    public string Company {
      get { return GetAttributeValue<AssemblyCompanyAttribute>(a => a.Company); }
    }

    protected string GetAttributeValue<TAttr>(Func<TAttr,
      string> resolveFunc, string defaultResult = null) where TAttr : Attribute {
      object[] attributes = _assembly.GetCustomAttributes(typeof (TAttr), false);
      if (attributes.Length > 0)
        return resolveFunc((TAttr) attributes[0]);
      else
        return defaultResult;
    }
  }
}
