using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class MainWindowM : ObservableObject {
    private int _activeLayout;
    private bool _isFullScreen;

    public Core CoreM { get; }

    public int ActiveLayout { get => _activeLayout; set { _activeLayout = value; OnPropertyChanged(); } }
    public bool CanOpenStatusPanel => CoreM.ThumbnailsGridsM.Current != null || CoreM.MediaViewerM.IsVisible;
    public bool IsFullScreenIsChanging { get; set; }
    public bool IsFullScreen {
      get => _isFullScreen;
      set {
        IsFullScreenIsChanging = true;
        _isFullScreen = value;
        ActiveLayout = value ? 1 : 0;
        OnPropertyChanged();
        IsFullScreenIsChanging = false;
      }
    }

    public MainWindowM(Core coreM) {
      CoreM = coreM;
    }
  }
}
