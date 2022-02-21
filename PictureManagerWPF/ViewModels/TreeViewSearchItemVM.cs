using MH.UI.WPF.Interfaces;

namespace PictureManager.ViewModels {
  public sealed class TreeViewSearchItemVM {
    public string IconName { get; }
    public string Title { get; }
    public string ToolTip { get; }
    public ICatTreeViewItem Item { get; }

    public TreeViewSearchItemVM(string iconName, string title, string toolTip, ICatTreeViewItem item) {
      IconName = iconName;
      Title = title;
      ToolTip = toolTip;
      Item = item;
    }
  }
}
