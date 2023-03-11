using MH.Utils.BaseClasses;
using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Controls {
  public class WindowButtons: Control {
    public static readonly DependencyProperty WindowProperty =
      DependencyProperty.Register(nameof(Window), typeof(Window), typeof(WindowButtons));

    public Window Window {
      get => (Window)GetValue(WindowProperty);
      set => SetValue(WindowProperty, value);
    }

    public RelayCommand<object> MinimizeWindowCommand { get; set; }
    public RelayCommand<object> MaximizeWindowCommand { get; set; }
    public RelayCommand<object> RestoreWindowCommand { get; set; }
    public RelayCommand<object> CloseWindowCommand { get; set; }

    static WindowButtons() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(WindowButtons),
        new FrameworkPropertyMetadata(typeof(WindowButtons)));
    }

    public override void OnApplyTemplate() {
      MinimizeWindowCommand = new(() => SystemCommands.MinimizeWindow(Window));
      MaximizeWindowCommand = new(() => SystemCommands.MaximizeWindow(Window));
      RestoreWindowCommand = new(() => SystemCommands.RestoreWindow(Window));
      CloseWindowCommand = new(() => SystemCommands.CloseWindow(Window));
    }
  }
}
