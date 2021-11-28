using System.Windows.Controls;

namespace PictureManager.Interfaces {
  public interface IMainTabsItem {
    string IconName { get; set; }
    string Title { get; set; }
    object ToolTip { get; set; }
    ContextMenu ContextMenu { get; set; }
  }
}