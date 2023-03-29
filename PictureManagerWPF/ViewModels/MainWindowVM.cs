using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Properties;
using System.Windows;

namespace PictureManager.ViewModels {
  public sealed class MainWindowVM : ObservableObject {
    private bool _isMaximized = true;
    private bool _fixWindowSize = true;

    public Core CoreM { get; }
    public AppCore CoreVM { get; }
    public MainWindowM MainWindowM { get; }

    public bool IsMaximized { get => _isMaximized; set { _isMaximized = value; OnPropertyChanged(); } }
    public bool FixWindowSize { get => _fixWindowSize; set { _fixWindowSize = value; OnPropertyChanged(); } }

    public RelayCommand<object> OpenSettingsCommand { get; }
    public RelayCommand<object> TestButtonCommand { get; }
    public RelayCommand<object> LoadedCommand { get; }
    public RelayCommand<Window> WindowStateChangedCommand { get; }

    public MainWindowVM(Core coreM, AppCore coreVM, MainWindowM model) {
      CoreM = coreM;
      CoreVM = coreVM;
      MainWindowM = model;

      OpenSettingsCommand = new(OpenSettings);
      TestButtonCommand = new(() => Tests.Run());
      LoadedCommand = new(Loaded);
      WindowStateChangedCommand = new(WindowStateChanged);

      AttachEvents();
    }

    private void AttachEvents() {
      MainWindowM.PropertyChanged += (_, e) => {
        if (nameof(MainWindowM.IsFullScreen).Equals(e.PropertyName))
          OnFullScreenChanged(App.MainWindowV, MainWindowM.IsFullScreen);
      };
    }

    private static void OnFullScreenChanged(Window window, bool isFullScreen) {
      // INFO for smoother transition from/to full screen
      // Adjust the appearance and performance of Windows
      // Disable => Animate windows when minimizing and maximizing

      var top = window.Top;
      var left = window.Left;
      var width = window.Width;
      var height = window.Height;

      window.Top = 0;
      window.Left = 0;
      window.Width = SystemParameters.PrimaryScreenWidth;
      window.Height = SystemParameters.PrimaryScreenHeight;
      window.WindowState = WindowState.Normal;
      window.ResizeMode = isFullScreen ? ResizeMode.NoResize : ResizeMode.CanResize;
      window.WindowStyle = isFullScreen ? WindowStyle.None : WindowStyle.SingleBorderWindow;
      window.WindowState = WindowState.Maximized;

      window.Top = top;
      window.Left = left;
      window.Width = width;
      window.Height = height;
    }

    private void WindowStateChanged(Window window) {
      IsMaximized = window.WindowState == WindowState.Maximized;
      FixWindowSize = window.WindowState == WindowState.Maximized && !MainWindowM.IsFullScreen;
    }

    private static void OpenSettings() {
      var result = Core.DialogHostShow(new SettingsDialogM());
      if (result == 0)
        Settings.Default.Save();
      else
        Settings.Default.Reload();
    }

    private void Loaded() {
      var windowsDisplayScale = 1.0;

      if (Application.Current.MainWindow != null)
        windowsDisplayScale = PresentationSource.FromVisual(Application.Current.MainWindow)
        ?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

      MainWindowM.Loaded(windowsDisplayScale);
    }
  }
}
