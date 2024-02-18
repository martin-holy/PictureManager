using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;

namespace PictureManager.Domain.Models; 

public sealed class TreeViewSearchItemM(string icon, string name, ITreeItem data, string toolTip, TreeCategory category)
  : ListItem(icon, name, data) {
  public string ToolTip { get; } = toolTip;
  public TreeCategory Category { get; } = category;
}