using PictureManager.ViewModel;

namespace PictureManager.Database {
  public class FavoriteFolder : BaseTreeViewItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; set; }
    public Folder Folder { get; set; }

    public FavoriteFolder(int id) {
      Id = id;
      IconName = IconName.Folder;
    }

    public string ToCsv() {
      // ID|Folder
      return string.Join("|", Id.ToString(), Folder.Id.ToString());
    }
  }
}
