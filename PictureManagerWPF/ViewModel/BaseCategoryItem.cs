using System;
using System.Linq;
using System.Windows;
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
      var dmCategoryGroup = new DataModel.CategoryGroup {
        Id = ACore.Db.GetNextIdFor<DataModel.CategoryGroup>(),
        Name = name,
        Category = (int) Category
      };

      ACore.Db.Insert(dmCategoryGroup);

      var vmCategoryGroup = new CategoryGroup(dmCategoryGroup) {IconName = CategoryGroupIconName, Parent = this};
      GroupSetInPalce(vmCategoryGroup, true);
      return vmCategoryGroup;
    }

    private void GroupSetInPalce(CategoryGroup group, bool isNew) {
      var idx = ACore.Db.CategoryGroups.Where(x => x.Category == (int) Category).OrderBy(x => x.Name).ToList().IndexOf(group.Data);
      if (isNew)
        Items.Insert(idx, group);
      else
        Items.Move(Items.IndexOf(group), idx);
    }

    public void GroupNewOrRename(CategoryGroup group, bool rename) {
      var inputDialog = new InputDialog {
        Owner = AppCore.WMain,
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
        if (root.Items.Where(x => x is CategoryGroup).SingleOrDefault(x => x.Title.Equals(inputDialog.Answer)) != null) {
          inputDialog.ShowErrorMessage("Group's name already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) {
          group.Data.Name = inputDialog.Answer;
          ACore.Db.Update(group.Data);
          GroupSetInPalce(group, false);
        } else GroupCreate(inputDialog.Answer);
      }
    }

    public void GroupDelete(CategoryGroup group) {
      var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (result != MessageBoxResult.Yes) return;
      var lists = DataModel.PmDataContext.GetInsertUpdateDeleteLists();

      foreach (var cgi in ACore.Db.CategoryGroupsItems.Where(x => x.CategoryGroupId == group.Data.Id)) {
        DataModel.PmDataContext.DeleteOnSubmit(cgi, lists);
      }

      DataModel.PmDataContext.DeleteOnSubmit(group.Data, lists);
      ACore.Db.SubmitChanges(lists);

      foreach (var item in group.Items) {
        Items.Add(item);
      }

      group.Items.Clear();
      Items.Remove(group);
    }

    public void LoadGroups() {
      foreach (var g in ACore.Db.CategoryGroups.Where(x => x.Category == (int) Category).OrderBy(x => x.Name)
        .Select(x => new CategoryGroup(x) {IconName = GetCategoryGroupIconName(), Parent = this})) {
        Items.Add(g);
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

    public void ItemMove(BaseTreeViewTagItem item, BaseTreeViewItem dest, int itemId) {
      var srcGroup = item.Parent as CategoryGroup;
      var destGroup = dest as CategoryGroup;
      var cgi = ACore.Db.CategoryGroupsItems.SingleOrDefault(x => x.ItemId == itemId && x.CategoryGroupId == srcGroup?.Data.Id);
      if (cgi != null) {
        if (destGroup != null) {
          cgi.CategoryGroupId = destGroup.Data.Id;
          ACore.Db.Update(cgi);
        } else {
          ACore.Db.Delete(cgi);
        }
      } else {
        if (destGroup != null) {
          InsertCategoryGroupItem(destGroup, itemId);
        }
      }

      item.Parent.Items.Remove(item);
      item.Parent = dest;
      ItemSetInPlace(dest, true, item);
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

    public InputDialog ItemGetInputDialog(BaseTreeViewItem item, IconName iconName, string itemName, bool rename) {
      var title = rename ? $"Rename {itemName}" : $"New {itemName}";
      var question = rename ? $"Enter the new name for the {itemName.ToLower()}." : $"Enter the name of the new {itemName.ToLower()}.";
      var error = $"{itemName}'s name already exists!";

      var inputDialog = new InputDialog {
        Owner = AppCore.WMain,
        IconName = iconName,
        Title = title,
        Question = question,
        Answer = rename ? item.Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (rename && string.Compare(inputDialog.Answer, item.Title, StringComparison.OrdinalIgnoreCase) == 0) {
          inputDialog.DialogResult = true;
          return;
        }

        var root = rename ? item.Parent : item;
        if (root.Items.Where(x => !(x is CategoryGroup)).SingleOrDefault(x => x.Title.Equals(inputDialog.Answer)) != null) {
          inputDialog.ShowErrorMessage(error);
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      return inputDialog;
    }

    #endregion

    public void InsertCategoryGroupItem(BaseTreeViewItem root, int itemId) {
      var cg = root as CategoryGroup;
      if (cg == null) return;
      var dmCategoryGroupItem = new DataModel.CategoryGroupItem {
        Id = ACore.Db.GetNextIdFor<DataModel.CategoryGroupItem>(),
        CategoryGroupId = cg.Data.Id,
        ItemId = itemId
      };
      ACore.Db.Insert(dmCategoryGroupItem);
    }
  }
}
