using MH.Utils.BaseClasses;

namespace PictureManager.Domain.HelperClasses {
  public class ItemsGroupInfoItem : ObservableObject {
    private string _icon;
    private string _title;
    private string _toolTip;

    public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string ToolTip { get => _toolTip; set { _toolTip = value; OnPropertyChanged(); } }
  }
}
