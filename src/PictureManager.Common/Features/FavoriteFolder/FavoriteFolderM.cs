﻿using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Folder;
using System;

namespace PictureManager.Common.Features.FavoriteFolder;

public sealed class FavoriteFolderM(int id, string name, FolderM folder) : TreeItem(Res.IconFolder, name), IEquatable<FavoriteFolderM> {
  private FolderM _folder = folder;

  #region IEquatable implementation
  public bool Equals(FavoriteFolderM? other) => Id == other?.Id;
  public override bool Equals(object? obj) => Equals(obj as FavoriteFolderM);
  public override int GetHashCode() => Id;
  public static bool operator ==(FavoriteFolderM? a, FavoriteFolderM? b) {
    if (ReferenceEquals(a, b)) return true;
    if (a is null || b is null) return false;
    return a.Equals(b);
  }
  public static bool operator !=(FavoriteFolderM? a, FavoriteFolderM? b) => !(a == b);
  #endregion

  public int Id { get; } = id;
  public FolderM Folder { get => _folder; set { _folder = value; OnPropertyChanged(); } }
}