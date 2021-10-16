using PictureManager.Domain.CatTreeViewModels;

namespace PictureManager.ViewModels.Tree {
  public class KeywordTreeVM : CatTreeViewItem, ICatTreeViewTagItem {
    public KeywordBaseVM BaseVM { get; }

    public KeywordTreeVM(KeywordBaseVM baseVM, ICatTreeViewItem parent) {
      BaseVM = baseVM;
      Parent = parent;
    }
  }
}
