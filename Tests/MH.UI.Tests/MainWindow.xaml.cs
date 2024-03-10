using System.Collections.Generic;
using System.Windows;

namespace MH.UI.Tests {
  public partial class MainWindow : Window {
    public List<object> TestList { get; } = new();

    public MainWindow() {
      InitializeComponent();

      for (int i = 0; i < 10; i++) {
        TestList.Add(i);
      }
    }
  }
}
