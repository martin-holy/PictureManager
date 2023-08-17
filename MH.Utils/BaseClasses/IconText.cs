using MH.Utils.Interfaces;

namespace MH.Utils.BaseClasses {
  public class IconText : ObservableObject, IIconText, ITitled {
    private string _iconName;
    private string _name;

    public string IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(GetTitle)); } }
    public string GetTitle => Name;

    public IconText(string icon, string text) {
      IconName = icon;
      Name = text;
    }
  }
}
