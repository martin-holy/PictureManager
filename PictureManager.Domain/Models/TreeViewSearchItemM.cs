using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;

namespace PictureManager.Domain.Models; 

public sealed class TreeViewSearchItemM : ListItem {
  public string ToolTip { get; }
  public TreeCategory Category { get; }

  public TreeViewSearchItemM(string icon, string name, ITreeItem data, string toolTip, TreeCategory category) : base(icon, name, data) {
    ToolTip = toolTip;
    Category = category;
  }
}