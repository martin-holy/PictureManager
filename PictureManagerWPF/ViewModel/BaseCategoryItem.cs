using System;
using System.Linq;
using PictureManager.Database;
using PictureManager.Dialogs;

namespace PictureManager.ViewModel {
  public class BaseCategoryItem : BaseTreeViewItem {
    public Category Category;
    public bool CanHaveGroups;
    public bool CanHaveSubItems;
    public bool CanModifyItems;
    public IconName CategoryGroupIconName;

    public BaseCategoryItem(Category category) {
      Category = category;
      CategoryGroupIconName = GetCategoryGroupIconName();
    }

    #region Group
    public CategoryGroup GroupCreate(string name) {
      var cg = new CategoryGroup(App.Core.CategoryGroups.Helper.GetNextId(), name, Category) {
        IconName = CategoryGroupIconName,
        Parent = this
      };

      App.Core.CategoryGroups.AddRecord(cg);
      GroupSetInPlace(cg, true);
      App.Core.Sdb.SaveAllTables();
      return cg;
    }

    private void GroupSetInPlace(CategoryGroup group, bool isNew) {
      var idx = App.Core.CategoryGroups.All.Where(x => x.Category == Category).OrderBy(x => x.Title).ToList().IndexOf(group);
      
      if (isNew)
        Items.Insert(idx, group);
      else
        Items.Move(Items.IndexOf(group), idx);
    }

    public void GroupNewOrRename(CategoryGroup group, bool rename) {
      var inputDialog = new InputDialog {
        Owner = App.WMain,
        IconName = CategoryGroupIconName,
        Title = rename ? "Rename Group" : "New Group",
        Question = rename ? "Enter the new name for the group." : "Enter the name of the new group.",
        Answer = rename ? group.Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (rename && string.Compare(inputDialog.TxtAnswer.Text, group.Title, StringComparison.OrdinalIgnoreCase) == 0) {
          inputDialog.DialogResult = true;
          return;
        }

        var root = rename ? group.Parent : this;
        if (root.Items.OfType<CategoryGroup>().SingleOrDefault(x => x.Title.Equals(inputDialog.Answer)) != null) {
          inputDialog.ShowErrorMessage("Group's name already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (!(inputDialog.ShowDialog() ?? true)) return;
      if (rename) {
        group.Title = inputDialog.Answer;
        GroupSetInPlace(group, false);
        App.Core.CategoryGroups.Helper.IsModified = true;
      } else GroupCreate(inputDialog.Answer);
    }

    public void GroupDelete(CategoryGroup group) {
      if (!MessageDialog.Show("Delete Confirmation", "Are you sure?", true)) return;

      // move Group items to the root
      foreach (var item in group.Items)
        Items.Add(item);

      group.Items.Clear();
      Items.Remove(group);
      App.Core.CategoryGroups.DeleteRecord(group);
    }

    public void LoadGroups() {
      foreach (var cg in App.Core.CategoryGroups.All.Where(x => x.Category == Category).OrderBy(x => x.Title)) {
        cg.IconName = GetCategoryGroupIconName();
        Items.Add(cg);
      }
    }

    private IconName GetCategoryGroupIconName() {
      switch (Category) {
        case Category.FavoriteFolders: return IconName.FolderStar;
        case Category.Folders: return IconName.Folder;
        case Category.Ratings: return IconName.Star;
        case Category.People: return IconName.PeopleMultiple;
        case Category.FolderKeywords: return IconName.Folder;
        case Category.Keywords: return IconName.TagLabel;
        case Category.Filters: return IconName.Filter;
        case Category.Viewers: return IconName.Eye;
        case Category.SqlQueries: return IconName.DatabaseSql;
        default: return IconName.Bug;
      }
    }
    #endregion

    #region Item

    public virtual void ItemNewOrRename(BaseTreeViewItem item, bool rename) {
      //implemented in inherited class
    }

    public virtual void ItemDelete(BaseTreeViewItem item) {
      //implemented in inherited class
    }

    public void ItemMove(BaseTreeViewTagItem item, BaseTreeViewItem dest) {
      item.Parent.Items.Remove(item);
      item.Parent = dest;
      ItemSetInPlace(dest, true, item);

      if (item.Parent is CategoryGroup || dest is CategoryGroup)
        App.Core.CategoryGroups.Helper.IsModified = true;
    }

    public void ItemSetInPlace(BaseTreeViewItem root, bool isNew, BaseTreeViewItem item) {
      var idx = root.Items.Where(x => !(x is CategoryGroup)).Select(x => x.Title).ToList().BinarySearch(item.Title);
      if (idx >= 0) return;
      idx = ~idx + root.Items.Count(x => x is CategoryGroup);
      if (isNew)
        root.Items.Insert(idx, item);
      else
        root.Items.Move(root.Items.IndexOf(item), idx);
    }

    public static InputDialog ItemGetInputDialog(BaseTreeViewItem item, IconName iconName, string itemName, bool rename) {
      var title = rename ? $"Rename {itemName}" : $"New {itemName}";
      var question = rename ? $"Enter the new name for the {itemName.ToLower()}." : $"Enter the name of the new {itemName.ToLower()}.";
      var inputDialog = new InputDialog {
        Owner = App.WMain,
        IconName = iconName,
        Title = title,
        Question = question,
        Answer = rename ? item.Title : string.Empty
      };

      inputDialog.TxtAnswer.SelectAll();

      return inputDialog;
    }

    #endregion
  }
}
