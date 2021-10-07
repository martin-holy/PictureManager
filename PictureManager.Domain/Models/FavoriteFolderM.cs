using PictureManager.Domain.Extensions;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class FavoriteFolderM : ObservableObject, IRecord {
    private Folder _folder;
    private string _title;

    public int Id { get; }
    public Folder Folder { get => _folder; set { _folder = value; OnPropertyChanged(); } }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string[] Csv { get; set; }

    public FavoriteFolderM(int id) {
      Id = id;
    }
  }
}
