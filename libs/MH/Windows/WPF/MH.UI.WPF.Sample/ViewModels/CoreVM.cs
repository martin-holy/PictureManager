using MH.Utils.BaseClasses;

namespace MH.UI.WPF.Sample.ViewModels;

public class CoreVM : ObservableObject {
  public MainWindowVM MainWindow { get; } = new();
}