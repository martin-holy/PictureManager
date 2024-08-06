using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.Keyword;

/// <summary>
/// DB fields: ID|Name|Parent
/// </summary>
public class KeywordM : TreeItem, IEquatable<KeywordM> {
  #region IEquatable implementation
  public bool Equals(KeywordM? other) => Id == other?.Id;
  public override bool Equals(object? obj) => Equals(obj as KeywordM);
  public override int GetHashCode() => Id;
  public static bool operator ==(KeywordM? a, KeywordM? b) {
    if (ReferenceEquals(a, b)) return true;
    if (a is null || b is null) return false;
    return a.Equals(b);
  }
  public static bool operator !=(KeywordM? a, KeywordM? b) => !(a == b);
  #endregion
    
  public int Id { get; }
  public string FullName => this.GetFullName("/", x => x.Name);

  public KeywordM(int id, string name, ITreeItem? parent) : base(Res.IconTag, name) {
    Id = id;
    Parent = parent;
  }

  public bool Toggle<T>(T[] items, Action<T> itemAction, Action? changedAction) where T : class, IHaveKeywords {
    if (items.Length == 0) return false;
    foreach (var item in items) {
      item.Keywords = item.Keywords.Toggle(this);
      itemAction?.Invoke(item);
    }
    changedAction?.Invoke();
    return true;
  }
}

public static class KeywordExtensions {
  public static IEnumerable<KeywordM> GetKeywords(this IEnumerable<KeywordM> keywords) =>
    keywords
      .EmptyIfNull()
      .SelectMany(x => x.GetThisAndParents())
      .Distinct();
}