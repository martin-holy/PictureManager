using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public class FavoriteFolder : CatTreeViewItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; }
    public Folder Folder { get; set; }

    public FavoriteFolder(int id) {
      Id = id;
      IconName = IconName.Folder;
    }

    // ID|FolderId|Title
    public string ToCsv() => string.Join("|", Id.ToString(), Folder.Id.ToString(), Title);
  }
}
