using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class MainWindowM : ObservableObject {
    private int _activeLayout;
    private bool _isFullScreen;

    public int ActiveLayout { get => _activeLayout; set { _activeLayout = value; OnPropertyChanged(); } }
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
  }
}
