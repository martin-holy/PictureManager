using System.Collections.ObjectModel;

namespace PictureManager.Domain.CatTreeViewModels {
  public interface ICatTreeViewBaseItem {
    ICatTreeViewBaseItem Parent { get; set; }
    ObservableCollection<ICatTreeViewBaseItem> Items { get; set; }
    IconName IconName { get; set; }
    string Title { get; set; }
    bool IsExpanded { get; set; }
    bool IsSelected { get; set; }
    object Tag { get; set; }
    string ToolTip { get; set; }
    BackgroundBrush BackgroundBrush { get; set; }
  }
}
