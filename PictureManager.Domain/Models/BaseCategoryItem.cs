using System.Linq;

namespace PictureManager.Domain.Models {
  public class BaseCategoryItem : BaseTreeViewItem {
    public Category Category { get; }
    public bool CanHaveGroups { get; set; }
    public bool CanHaveSubItems { get; set; }
    public bool CanModifyItems { get; set; }
    public IconName CategoryGroupIconName { get; }

    public BaseCategoryItem(Category category) {
      Category = category;
      CategoryGroupIconName = GetCategoryGroupIconName();
    }

    #region Group
    public CategoryGroup GroupCreate(string name) {
      var cg = new CategoryGroup(Core.Instance.CategoryGroups.Helper.GetNextId(), name, Category) {
        IconName = CategoryGroupIconName,
        Parent = this
      };

      Core.Instance.CategoryGroups.AddRecord(cg);
      GroupSetInPlace(cg, true);
      Core.Instance.Sdb.SaveAllTables();
      return cg;
    }

    public void GroupRename(CategoryGroup group, string newTitle) {
      group.Title = newTitle;
      GroupSetInPlace(group, false);
      Core.Instance.CategoryGroups.Helper.IsModified = true;
    }

    public void GroupDelete(CategoryGroup group) {
      // move Group items to the root
      foreach (var item in group.Items)
        Items.Add(item);

      group.Items.Clear();
      Items.Remove(group);
      Core.Instance.CategoryGroups.DeleteRecord(group);
    }

    public void GroupSetInPlace(CategoryGroup group, bool isNew) {
      var idx = Core.Instance.CategoryGroups.All.Where(x => x.Category == Category).OrderBy(x => x.Title).ToList().IndexOf(group);
      
      if (isNew)
        Items.Insert(idx, group);
      else
        Items.Move(Items.IndexOf(group), idx);
    }

    public string ValidateNewGroupTitle (string newTitle) {
      return Items.OfType<CategoryGroup>().SingleOrDefault(x => x.Title.Equals(newTitle)) != null
        ? "Group's name already exists!"
        : null;
    }

    public void LoadGroups() {
      foreach (var cg in Core.Instance.CategoryGroups.All.Where(x => x.Category == Category).OrderBy(x => x.Title)) {
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
    public void ItemMove(BaseTreeViewTagItem item, BaseTreeViewItem dest) {
      item.Parent.Items.Remove(item);
      item.Parent = dest;
      ItemSetInPlace(dest, true, item);

      if (item.Parent is CategoryGroup || dest is CategoryGroup)
        Core.Instance.CategoryGroups.Helper.IsModified = true;
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
    #endregion
  }
}
