using MH.Utils.Interfaces;

namespace MH.UI.WPF.Interfaces {
  public interface ICatTreeViewItem : ITreeBranch {
    bool IsExpanded { get; set; }
    bool IsSelected { get; set; }
    bool IsHidden { get; set; }
  }
}
