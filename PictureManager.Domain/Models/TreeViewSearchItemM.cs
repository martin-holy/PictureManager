using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class TreeViewSearchItemM : ListItem {
    public string ToolTip { get; }
    public TreeCategoryBase Category { get; }

    public TreeViewSearchItemM(string icon, string name, ITreeItem data, string toolTip, TreeCategoryBase category) : base(icon, name, data) {
      ToolTip = toolTip;
      Category = category;
    }
  }
}
