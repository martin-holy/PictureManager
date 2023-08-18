using MH.Utils.BaseClasses;
using PictureManager.Domain.Dialogs;

namespace PictureManager.Domain.Models {
  public sealed class MainWindowM : ObservableObject {
    private int _activeLayout;
    private bool _isFullScreen;

    public Core CoreM { get; }

    public int ActiveLayout { get => _activeLayout; set { _activeLayout = value; OnPropertyChanged(); } }
    public bool CanOpenStatusPanel => CoreM.MediaItemsViews.Current != null || CoreM.MediaViewerM.IsVisible;
    public bool IsFullScreen {
      get => _isFullScreen;
      set {
        _isFullScreen = value;
        ActiveLayout = value ? 1 : 0;
        OnPropertyChanged();
      }
    }

    public RelayCommand<object> OpenAboutCommand { get; }
    public RelayCommand<object> ClosingCommand { get; }
    public RelayCommand<object> SwitchToBrowserCommand { get; }
    public RelayCommand<object> SaveDbCommand { get; }
    public RelayCommand<object> OpenLogCommand { get; }
    public RelayCommand<object> OpenSettingsCommand { get; }

    public MainWindowM(Core coreM) {
      CoreM = coreM;

      OpenAboutCommand = new(OpenAbout);
      ClosingCommand = new(Closing);
      SwitchToBrowserCommand = new(
        () => IsFullScreen = false,
        () => CoreM.MediaViewerM.IsVisible);
      SaveDbCommand = new(
        () => CoreM.Sdb.SaveAllTables(),
        () => CoreM.Sdb.Changes > 0);
      OpenLogCommand = new(OpenLog);
      OpenSettingsCommand = new(OpenSettings);
    }

    private static void OpenAbout() {
      Core.DialogHostShow(new AboutDialogM());
    }

    private void Closing() {
      CoreM.SaveMetadataPrompt();
      CoreM.SaveDBPrompt();
      CoreM.Sdb.BackUp();
    }

    private static void OpenLog() {
      Core.DialogHostShow(new LogDialogM());
    }

    private static void OpenSettings() {
      var result = Core.DialogHostShow(new SettingsDialogM());
      if (result == 0)
        Core.Settings.Save();
      else
        Core.Settings.Load();
    }
  }
}
