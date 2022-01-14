using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class FavoriteFolderM : ObservableObject, IRecord, ITreeLeaf {
    #region ITreeLeaf implementation
    public ITreeBranch Parent { get; set; }
    #endregion

    private FolderM _folder;
    private string _title;

    public int Id { get; }
    public FolderM Folder { get => _folder; set { _folder = value; OnPropertyChanged(); } }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string[] Csv { get; set; }

    public FavoriteFolderM(int id) {
      Id = id;
    }
  }
}
