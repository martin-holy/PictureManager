using System.Linq;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using MH.Utils;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Interfaces;
using PictureManager.ViewModels.Tree;

namespace PictureManager.Commands {
  public static class TreeViewCommands {
    public static RelayCommand<ICatTreeViewItem> TagItemDeleteNotUsedCommand { get; } =
      new(TagItemDeleteNotUsed, item => item != null);

    public static RelayCommand<IFilterItem> ActivateFilterAndCommand { get; } = 
      new(item => _ = App.Ui.ActivateFilter(item, DisplayFilter.And), item => item != null);

    public static RelayCommand<IFilterItem> ActivateFilterOrCommand { get; } =
      new(item => _ = App.Ui.ActivateFilter(item, DisplayFilter.Or), item => item != null);

    public static RelayCommand<IFilterItem> ActivateFilterNotCommand { get; } =
      new(item => _ = App.Ui.ActivateFilter(item, DisplayFilter.Not), item => item != null);

    public static RelayCommand<ICatTreeViewItem> LoadByTagCommand { get; } =
      new(item => _ = App.Ui.TreeView_Select(item, true), item => item != null);

    private static void TagItemDeleteNotUsed(ICatTreeViewItem root) {
      if (Tree.GetTopParent(root) is not CatTreeViewCategoryBase cat) return;
      if (!MessageDialog.Show("Delete Confirmation",
        $"Do you really want to delete not used items in '{cat.GetTitle(root)}'?", true)) return;

      switch (cat.Category) {
        case Category.People: 
          App.Core.PeopleM.DeleteNotUsed(root.Items.OfType<PersonTreeVM>().Select(x => x.BaseVM.Model));
          break;

        case Category.Keywords:
          App.Core.KeywordsM.DeleteNotUsed(root.Items.OfType<KeywordTreeVM>().Select(x => x.BaseVM.Model));
          break;
      }
    }
  }
}
