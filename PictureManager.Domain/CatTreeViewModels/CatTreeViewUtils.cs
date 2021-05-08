namespace PictureManager.Domain.CatTreeViewModels {
  public static class CatTreeViewUtils {
    public static IconName GetCategoryGroupIconName(Category category) {
      switch (category) {
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

    public static ICatTreeViewBaseItem GetTopParent(ICatTreeViewBaseItem item) {
      if (item == null) return null;
      while (true) {
        if (item.Parent == null) return item;
        item = item.Parent;
      }
    }
  }
}
