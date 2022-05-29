using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class MainWindowM : ObservableObject {
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
  }
}
