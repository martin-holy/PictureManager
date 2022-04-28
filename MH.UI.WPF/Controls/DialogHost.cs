using System;
using MH.Utils.Interfaces;
using System.Windows;

namespace MH.UI.WPF.Controls {
  public class DialogHost : Window {
    static DialogHost() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(DialogHost),
        new FrameworkPropertyMetadata(typeof(DialogHost)));
    }

    private DialogHost(IDialog content) {
      Content = content;
      Owner = Application.Current.MainWindow;

      // I had problem with setting this in XAML
      WindowStartupLocation = WindowStartupLocation.CenterOwner;

      content.PropertyChanged += (_, e) => {
        if (nameof(content.Result).Equals(e.PropertyName, StringComparison.Ordinal))
          Close();
      };
    }

    public static int Show(IDialog content) {
      var dh = new DialogHost(content);
      dh.ShowDialog();

      return content.Result;
    }
  }
}
