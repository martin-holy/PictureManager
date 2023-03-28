using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Properties;
using System;
using System.Windows;

namespace PictureManager.ViewModels {
  public sealed class MainWindowVM : ObservableObject {
    private bool _isMaximized = true;
    private bool _fixWindowSize = true;

    public Core CoreM { get; }
    public AppCore CoreVM { get; }
    public MainWindowM Model { get; }

    public bool IsMaximized { get => _isMaximized; set { _isMaximized = value; OnPropertyChanged(); } }
    public bool FixWindowSize { get => _fixWindowSize; set { _fixWindowSize = value; OnPropertyChanged(); } }

    public RelayCommand<object> SwitchToBrowserCommand { get; }
    public RelayCommand<object> OpenSettingsCommand { get; }
    public RelayCommand<object> OpenAboutCommand { get; }
    public RelayCommand<object> OpenLogCommand { get; }
    public RelayCommand<object> TestButtonCommand { get; }
    public RelayCommand<object> SaveDbCommand { get; }
    public RelayCommand<object> ClosingCommand { get; }
    public RelayCommand<object> LoadedCommand { get; }
    public RelayCommand<Window> WindowStateChangedCommand { get; }

    public MainWindowVM(Core coreM, AppCore coreVM, MainWindowM model) {
      CoreM = coreM;
      CoreVM = coreVM;
      Model = model;

      SwitchToBrowserCommand = new(
        () => Model.IsFullScreen = false,
        () => CoreM.MediaViewerM.IsVisible);
      OpenSettingsCommand = new(OpenSettings);
      OpenAboutCommand = new(OpenAbout);
      OpenLogCommand = new(OpenLog);
      TestButtonCommand = new(() => Tests.Run());
      SaveDbCommand = new(
        () => CoreM.Sdb.SaveAllTables(),
        () => CoreM.Sdb.Changes > 0);
      ClosingCommand = new(Closing);
      LoadedCommand = new(Loaded);
      WindowStateChangedCommand = new(WindowStateChanged);

      AttachEvents();
    }

    private void AttachEvents() {
      Model.PropertyChanged += (_, e) => {
        if (nameof(Model.IsFullScreen).Equals(e.PropertyName))
          OnFullScreenChanged(App.MainWindowV, Model.IsFullScreen);
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
      FixWindowSize = window.WindowState == WindowState.Maximized && !Model.IsFullScreen;
    }

    private static void OpenSettings() {
      var result = Core.DialogHostShow(new SettingsDialogM());
      if (result == 0)
        Settings.Default.Save();
      else
        Settings.Default.Reload();
    }

    private static void OpenAbout() {
      Core.DialogHostShow(new AboutDialogM());
    }

    private static void OpenLog() {
      var log = new LogDialog { Owner = Application.Current.MainWindow };
      log.ShowDialog();
    }

    private void Closing() {
      if (CoreM.MediaItemsM.ModifiedItems.Count > 0 &&
          Core.DialogHostShow(new MessageDialog(
            "Metadata Edit",
            "Some Media Items are modified, do you want to save them?",
            Res.IconQuestion,
            true)) == 0) {
        CoreM.MediaItemsM.SaveEdit();
      }

      if (CoreM.Sdb.Changes > 0 &&
          Core.DialogHostShow(new MessageDialog(
            "Database changes",
            "There are some changes in database, do you want to save them?",
            Res.IconQuestion,
            true)) == 0) {
        CoreM.Sdb.SaveAllTables();
      }

      CoreM.Sdb.BackUp();
    }

    private void Loaded() {
      var windowsDisplayScale = 1.0;
      AppCore.ScrollBarSize = 14;

      if (Application.Current.MainWindow != null)
        windowsDisplayScale = PresentationSource.FromVisual(Application.Current.MainWindow)
        ?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

      CoreM.ThumbnailsGridsM.DefaultThumbScale = 1 / windowsDisplayScale;
      CoreVM.SegmentsVM.SegmentUiSize = CoreM.SegmentsM.SegmentSize / windowsDisplayScale;
      CoreM.MediaItemsM.OnPropertyChanged(nameof(CoreM.MediaItemsM.MediaItemsCount));
    }
  }
}
