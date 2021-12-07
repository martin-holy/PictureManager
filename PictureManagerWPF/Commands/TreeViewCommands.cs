using System.Linq;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using MH.Utils;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.ViewModels.Tree;

namespace PictureManager.Commands {
  public static class TreeViewCommands {
    // TODO move elsewhere
    public static RelayCommand<ICatTreeViewItem> TagItemDeleteNotUsedCommand { get; } =
      new(TagItemDeleteNotUsed, item => item != null);

    private static void TagItemDeleteNotUsed(ICatTreeViewItem root) {
      if (Tree.GetTopParent(root) is not CatTreeViewCategoryBase cat) return;
      if (!MessageDialog.Show("Delete Confirmation",
        $"Do you really want to delete not used items in '{cat.GetTitle(root)}'?", true)) return;

      switch (cat.Category) {
        case Category.People: 
          App.Core.PeopleM.DeleteNotUsed(root.Items.OfType<PersonTreeVM>().Select(x => x.Model));
          break;

        case Category.Keywords:
          App.Core.KeywordsM.DeleteNotUsed(root.Items.OfType<KeywordTreeVM>().Select(x => x.Model));
          break;
      }
    }
  }
}
