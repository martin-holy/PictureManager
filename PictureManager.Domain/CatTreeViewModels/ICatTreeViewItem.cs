using PictureManager.Domain.Interfaces;

namespace PictureManager.Domain.CatTreeViewModels {
  public interface ICatTreeViewItem : ITreeBranch {
    IconName IconName { get; set; }
    string Title { get; set; }
    bool IsExpanded { get; set; }
    bool IsSelected { get; set; }
    bool IsHidden { get; set; }
    bool IsMarked { get; set; }
    int PicCount { get; set; }
    object Tag { get; set; }
    string ToolTip { get; set; }
    BackgroundBrush BackgroundBrush { get; set; }
  }
}
