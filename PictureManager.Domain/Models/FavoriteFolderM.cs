using MH.Utils.BaseClasses;
using System;

namespace PictureManager.Domain.Models {
  public sealed class FavoriteFolderM : TreeItem, IEquatable<FavoriteFolderM> {
    #region IEquatable implementation
    public bool Equals(FavoriteFolderM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as FavoriteFolderM);
    public override int GetHashCode() => Id;
    public static bool operator ==(FavoriteFolderM a, FavoriteFolderM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(FavoriteFolderM a, FavoriteFolderM b) => !(a == b);
    #endregion

    private FolderM _folder;

    public int Id { get; }
    public FolderM Folder { get => _folder; set { _folder = value; OnPropertyChanged(); } }

    public FavoriteFolderM(int id, string name) : base(Res.IconFolder, name) {
      Id = id;
    }
  }
}
