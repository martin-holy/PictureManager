using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models;

/// <summary>
/// DB fields: ID|Name|Category|GroupItems
/// </summary>
public sealed class CategoryGroupM : TreeItem, IEquatable<CategoryGroupM>, ITreeGroup {
  #region IEquatable implementation
  public bool Equals(CategoryGroupM other) => Id == other?.Id;
  public override bool Equals(object obj) => Equals(obj as CategoryGroupM);
  public override int GetHashCode() => Id;
  public static bool operator ==(CategoryGroupM a, CategoryGroupM b) => a?.Equals(b) ?? b is null;
  public static bool operator !=(CategoryGroupM a, CategoryGroupM b) => !(a == b);
  #endregion

  public int Id { get; }
  public Category Category { get; }

  public CategoryGroupM(int id, string name, Category category, string icon) : base(icon, name) {
    Id = id;
    Category = category;
  }
}

public static class CategoryGroupExtensions {
  public static IEnumerable<CategoryGroupM> GetCategoryGroups<T>(this T item) where T : ITreeItem =>
    item.Parent is CategoryGroupM cg
      ? cg.GetThisAndParents()
      : Enumerable.Empty<CategoryGroupM>();
}