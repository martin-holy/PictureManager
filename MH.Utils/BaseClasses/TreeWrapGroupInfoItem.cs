using System;

namespace MH.Utils.BaseClasses {
  public class TreeWrapGroupInfoItem : ObservableObject {
    private string _icon;
    private string _title;
    private string _toolTip;

    public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string ToolTip { get => _toolTip; set { _toolTip = value; OnPropertyChanged(); } }

    public TreeWrapGroupInfoItem() { }

    public TreeWrapGroupInfoItem(string icon, string title) {
      Icon = icon;
      Title = title;
    }

    public TreeWrapGroupInfoItem(string icon, string title, string toolTip) : this(icon, title) {
      ToolTip = toolTip;
    }

    public static bool AreEqual(TreeWrapGroupInfoItem a, TreeWrapGroupInfoItem b) =>
      string.Equals(a.Icon, b.Icon, StringComparison.Ordinal) &&
      string.Equals(a.Title, b.Title, StringComparison.CurrentCulture) &&
      string.Equals(a.ToolTip, b.ToolTip, StringComparison.CurrentCulture);

    public static bool AreEqual(TreeWrapGroupInfoItem[] a, TreeWrapGroupInfoItem[] b) {
      if (a == null || b == null || a.Length != b.Length) return false;

      for (var i = 0; i < a.Length; i++)
        if (!AreEqual(a[i], b[i]))
          return false;

      return true;
    }
  }
}
