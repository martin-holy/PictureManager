using MH.Utils.BaseClasses;
using PictureManager.Domain.Dialogs;

namespace PictureManager.Domain.Models; 

public sealed class MainWindowM : ObservableObject {
  private int _activeLayout;
  private bool _isFullScreen;
  private bool _isInViewMode;

  // SlidePanelsGrid: Left, Top, Right, Bottom, FullScreen (not part of SlidePanelsGrid)
  public object[] PinLayouts { get; set; } = {
    new[] { true, true, false, true, false }, // browse mode
    new[] { false, false, false, true, false } // view mode
  };

  public int ActiveLayout { get => _activeLayout; set { _activeLayout = value; OnPropertyChanged(); } }

  public bool IsFullScreen {
    get => _isFullScreen;
    set {
      if (_isFullScreen == value) return;
      _isFullScreen = value;
      ((bool[])PinLayouts[ActiveLayout])[4] = value;
      OnPropertyChanged();
    }
  }

  public bool IsInViewMode {
    get => _isInViewMode;
    set {
      _isInViewMode = value;
      ActiveLayout = value ? 1 : 0;
      IsFullScreen = ((bool[])PinLayouts[ActiveLayout])[4];
      OnPropertyChanged();
    }
  }

  public RelayCommand OpenAboutCommand { get; }
  public RelayCommand ClosingCommand { get; }
  public RelayCommand SwitchToBrowserCommand { get; }
  public RelayCommand SaveDbCommand { get; }
  public RelayCommand OpenLogCommand { get; }
  public RelayCommand OpenSettingsCommand { get; }

  public MainWindowM() {
    OpenAboutCommand = new(OpenAbout, null, "About");
    ClosingCommand = new(Closing);
    SwitchToBrowserCommand = new(
      () => IsInViewMode = false,
      () => Core.MediaViewerM.IsVisible);
    SaveDbCommand = new(
      () => Core.Db.SaveAllTables(),
      () => Core.Db.Changes > 0);
    OpenLogCommand = new(OpenLog);
    OpenSettingsCommand = new(OpenSettings, Res.IconSettings, "Settings");
  }

  private static void OpenAbout() =>
    Dialog.Show(new AboutDialogM());

  private static void Closing() {
    Core.Inst.SaveDBPrompt();
    Core.Db.BackUp();
  }

  private static void OpenLog() =>
    Dialog.Show(new LogDialogM());

  private static void OpenSettings() =>
    Core.MainTabs.Activate(Res.IconSettings, "Settings", Core.Settings);
}