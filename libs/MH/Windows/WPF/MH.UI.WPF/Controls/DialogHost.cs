using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Controls;

public class DialogHost : ObservableObject {
  public Dialog Content { get; }
  public CustomWindow Window { get; }
  public DataTemplateSelector DialogTemplateSelector { get; }

  private DialogHost(Dialog content) {
    var owner = GeoOwner();

    Content = content;
    
    Window = new() {
      Content = this,
      Owner = owner,
      WindowStartupLocation = WindowStartupLocation.CenterOwner,
      ShowInTaskbar = false,
      CanResize = true,
      SizeToContent = SizeToContent.WidthAndHeight,
      Style = Application.Current.FindResource("MH.Styles.Controls.CustomWindow") as Style
    };

    DialogTemplateSelector = Application.Current.FindResource("MH.Styles.Controls.DialogHost.DialogTemplateSelector") as DataTemplateSelector;

    content.PropertyChanged += (_, e) => {
      if (e.Is(nameof(content.Result)))
        Window.Close();
    };
  }

  public static int Show(Dialog content) {
    var dh = new DialogHost(content);
    dh.Window.ShowDialog();

    return content.Result;
  }

  private static Window GeoOwner() =>
    Application.Current.Windows[^1]?.Content is DialogHost dh
      ? dh.Window
      : Application.Current.MainWindow;
}