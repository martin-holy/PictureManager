using MH.Utils.BaseClasses;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class FavoriteFolderM : TreeItem, IRecord {
    private FolderM _folder;

    public int Id { get; }
    public FolderM Folder { get => _folder; set { _folder = value; OnPropertyChanged(); } }

    public FavoriteFolderM(int id, string name) : base(Res.IconFolder, name) {
      Id = id;
    }
  }
}
