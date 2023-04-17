using MH.Utils.BaseClasses;
using System;
using System.Windows;

namespace MH.UI.WPF.Controls {
  public class DialogHost {
    public Dialog Content { get; }
    public CustomWindow Window { get; }

    private DialogHost(Dialog content) {
      Content = content;

      Window = new CustomWindow() {
        Content = this,
        Owner = Application.Current.MainWindow,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ShowInTaskbar = false,
        CanResize = true,
        SizeToContent = SizeToContent.WidthAndHeight,
        Style = Application.Current.FindResource("MH.Styles.Controls.CustomWindow") as Style
      };

      content.PropertyChanged += (_, e) => {
        if (nameof(content.Result).Equals(e.PropertyName, StringComparison.Ordinal))
          Window.Close();
      };
    }

    public static int Show(Dialog content) {
      var dh = new DialogHost(content);
      dh.Window.ShowDialog();

      return content.Result;
    }
  }
}
