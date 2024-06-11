using MH.UI.Controls;
using MH.Utils.BaseClasses;

namespace MH.UI.WPF.Sample.ViewModels.Layout;

public sealed class ToolBarVM : ObservableObject {
  private bool _checkedMenuItem = true;

  public SlidePanelPinButton SlidePanelPinButton { get; } = new();
  public bool CheckedMenuItem { get => _checkedMenuItem; set { _checkedMenuItem = value; OnPropertyChanged(); } }
  public RelayCommand SampleImageCommand { get; } = new(() => { }, Res.IconImage, "Sample image");
  public RelayCommand SampleVideoCommand { get; } = new(() => { }, Res.IconMovieClapper, "Sample video");
}