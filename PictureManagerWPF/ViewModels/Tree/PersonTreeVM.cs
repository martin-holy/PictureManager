using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public class PersonTreeVM : CatTreeViewItem, ICatTreeViewTagItem {
    public PersonBaseVM BaseVM { get; }

    public PersonTreeVM(PersonBaseVM baseVM, ITreeBranch parent) {
      BaseVM = baseVM;
      Parent = parent;
    }
  }
}
