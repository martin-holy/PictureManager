using MH.UI.WPF.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PictureManager.ViewModels {
  public sealed class MainWindowVM : ObservableObject {
    private readonly Core _core;
    private readonly AppCore _coreVM;
    private bool _isFullScreen;

    public bool IsFullScreenIsChanging { get; private set; }
    public bool IsFullScreen {
      get => _isFullScreen;
      set {
        IsFullScreenIsChanging = true;
        _isFullScreen = value;
        OnPropertyChanged();
        IsFullScreenIsChanging = false;
      }
    }

    public RelayCommand<object> SwitchToFullScreenCommand { get; }
    public RelayCommand<object> SwitchToBrowserCommand { get; }
    public RelayCommand<object> OpenSettingsCommand { get; }
    public RelayCommand<object> OpenAboutCommand { get; }
    public RelayCommand<object> OpenLogCommand { get; }
    public RelayCommand<object> TestButtonCommand { get; }
    public RelayCommand<object> SaveDbCommand { get; }
    public RelayCommand<object> ClosingCommand { get; }
    public RelayCommand<object> LoadedCommand { get; }

    public MainWindowVM(Core core, AppCore coreVM) {
      _core = core;
      _coreVM = coreVM;

      SwitchToFullScreenCommand = new(() => IsFullScreen = true);
      SwitchToBrowserCommand = new(() => IsFullScreen = false);
      OpenSettingsCommand = new(OpenSettings);
      OpenAboutCommand = new(OpenAbout);
      OpenLogCommand = new(OpenLog);
      TestButtonCommand = new(() => Tests.Run());
      SaveDbCommand = new(
        () => _core.Sdb.SaveAllTables(),
        () => _core.Sdb.Changes > 0);
      ClosingCommand = new(Closing);
      LoadedCommand = new(Loaded);
    }

    private static void OpenSettings() {
      var settings = new SettingsDialog { Owner = Application.Current.MainWindow };
      if (settings.ShowDialog() ?? true)
        Settings.Default.Save();
      else
        Settings.Default.Reload();
    }

    private static void OpenAbout() {
      var about = new AboutDialog { Owner = Application.Current.MainWindow };
      about.ShowDialog();
    }

    private static void OpenLog() {
      var log = new LogDialog { Owner = Application.Current.MainWindow };
      log.ShowDialog();
    }

    private void Closing() {
      if (_core.MediaItemsM.ModifiedItems.Count > 0 &&
          MessageDialog.Show(
            "Metadata Edit",
            "Some Media Items are modified, do you want to save them?",
            true)) {
        _coreVM.MediaItemsVM.SaveEdit();
      }

      if (_core.Sdb.Changes > 0 &&
          MessageDialog.Show(
            "Database changes",
            "There are some changes in database, do you want to save them?",
            true)) {
        _core.Sdb.SaveAllTables();
      }

      _core.Sdb.BackUp();
    }

    private void Loaded() {
      var windowsDisplayScale = PresentationSource.FromVisual(Application.Current.MainWindow)
        ?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

      _core.ThumbnailsGridsM.DefaultThumbScale = 1 / windowsDisplayScale;
      _coreVM.SegmentsVM.SegmentUiSize = (int)Math.Round(_core.SegmentsM.SegmentSize / windowsDisplayScale);
      _coreVM.SegmentsVM.SegmentUiFullWidth = _coreVM.SegmentsVM.SegmentUiSize + 6; //border, margin, padding, ... //TODO find the real value
    }
  }
}
