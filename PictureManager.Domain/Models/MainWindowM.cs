using MH.Utils.BaseClasses;
using PictureManager.Domain.Dialogs;

namespace PictureManager.Domain.Models; 

public sealed class MainWindowM : ObservableObject {
  private int _activeLayout;
  private bool _isFullScreen;

  public int ActiveLayout { get => _activeLayout; set { _activeLayout = value; OnPropertyChanged(); } }
  public bool CanOpenStatusPanel => Core.MediaItemsViews.Current != null || Core.MediaViewerM.IsVisible;
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

  public MainWindowM() {
    OpenAboutCommand = new(OpenAbout);
    ClosingCommand = new(Closing);
    SwitchToBrowserCommand = new(
      () => IsFullScreen = false,
      () => Core.MediaViewerM.IsVisible);
    SaveDbCommand = new(
      () => Core.Db.SaveAllTables(),
      () => Core.Db.Changes > 0);
    OpenLogCommand = new(OpenLog);
    OpenSettingsCommand = new(OpenSettings);
  }

  private static void OpenAbout() =>
    Dialog.Show(new AboutDialogM());

  private static void Closing() {
    Core.Instance.SaveMetadataPrompt();
    Core.Instance.SaveDBPrompt();
    Core.Db.BackUp();
  }

  private static void OpenLog() =>
    Dialog.Show(new LogDialogM());

  private static void OpenSettings() {
    var result = Dialog.Show(new SettingsDialogM());
    if (result == 0)
      Core.Settings.Save();
    else
      Core.Settings.Load();
  }
}