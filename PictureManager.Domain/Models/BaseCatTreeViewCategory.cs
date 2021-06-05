using System.Collections.Generic;
using System.Linq;
using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public class BaseCatTreeViewCategory: CatTreeViewCategory, ICatTreeViewCategory {
    public BaseCatTreeViewCategory(Category category) : base(category) { }

    public new ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      throw new System.NotImplementedException();
    }

    public new void ItemRename(ICatTreeViewItem item, string name) {
      if (!(CatTreeViewUtils.GetTopParent(item) is ICatTreeViewCategory cat) || !(cat is ITable table)) return;

      item.Title = name;

      var idx = CatTreeViewUtils.SetItemInPlace(item.Parent, item);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(table.All, item.Parent, idx);

      table.All.Move(item as IRecord, allIdx);
      table.SaveToFile();
    }

    public new void ItemDelete(ICatTreeViewItem item) {
      throw new System.NotImplementedException();
    }

    public new void ItemCopy(ICatTreeViewItem item, ICatTreeViewItem dest) {
      throw new System.NotImplementedException();
    }

    public new void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) {
      if (!(CatTreeViewUtils.GetTopParent(item) is ICatTreeViewCategory cat) || !(cat is ITable table)) return;

      var saveGroups = dest is ICatTreeViewCategory || dest is ICatTreeViewGroup || !Equals(item.Parent, dest.Parent);

      base.ItemMove(item, dest, aboveDest);
      var idx = item.Parent.Items.IndexOf(item);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(table.All, item.Parent, idx);

      table.All.Move(item as IRecord, allIdx);
      table.SaveToFile();
      if (saveGroups)
        Core.Instance.CategoryGroups.SaveToFile();
    }

    public new ICatTreeViewGroup GroupCreate(ICatTreeViewCategory cat, string name) {
      return Core.Instance.CategoryGroups.GroupCreate(cat, name);
    }

    public new void GroupRename(ICatTreeViewGroup group, string name) {
      Core.Instance.CategoryGroups.GroupRename(group, name);
    }

    public new void GroupDelete(ICatTreeViewGroup group) {
      base.GroupDelete(group);
      Core.Instance.CategoryGroups.GroupDelete(group as CategoryGroup);
    }

    public new void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest) {
      base.GroupMove(group, dest, aboveDest);
      Core.Instance.CategoryGroups.GroupMove(group as CategoryGroup, dest as CategoryGroup, aboveDest);
    }

    public void LoadGroupsAndItems(List<IRecord> items) {
      var recGroup = new Dictionary<int, CategoryGroup>();

      foreach (var group in Core.Instance.CategoryGroups.All.Cast<CategoryGroup>().Where(x => x.Category == Category)) {
        group.IconName = GetCategoryGroupIconName();
        group.Parent = this;
        Items.Add(group);

        if (!string.IsNullOrEmpty(group.Csv[3]))
          foreach (var itemId in group.Csv[3].Split(','))
            recGroup.Add(int.Parse(itemId), group);

        // csv array is not needed any more
        group.Csv = null;
      }

      foreach (var item in items.Cast<ICatTreeViewItem>().Where(x => x.Parent == null)) {
        var group = recGroup[((IRecord)item).Id];
        if (group != null) {
          item.Parent = group;
          group.Items.Add(item);
        }
        else {
          item.Parent = this;
          Items.Add(item);
        }
      }
    }

    public IconName GetCategoryGroupIconName() {
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
  }
}
