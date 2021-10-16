using PictureManager.Domain.CatTreeViewModels;

namespace PictureManager.ViewModels.Tree {
  public class PersonTreeVM : CatTreeViewItem, ICatTreeViewTagItem {
    public PersonBaseVM BaseVM { get; }

    public PersonTreeVM(PersonBaseVM baseVM, ICatTreeViewItem parent) {
      BaseVM = baseVM;
      Parent = parent;
    }
  }
}
