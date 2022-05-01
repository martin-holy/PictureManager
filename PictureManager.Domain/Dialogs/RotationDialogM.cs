using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;

namespace PictureManager.Domain.Dialogs {
  public sealed class RotationDialogM : ObservableObject, IDialog {
    private string _title;
    private int _result = 1;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public int Result { get => _result; set { _result = value; OnPropertyChanged(); } }
    public RelayCommand<MediaOrientation> RotationCommand { get; }

    public RotationDialogM() {
      Title = "Rotation";
      RotationCommand = new(mo => Result = (int)mo);
    }
  }
}
