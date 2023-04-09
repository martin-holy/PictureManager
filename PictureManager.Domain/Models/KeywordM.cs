using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Interfaces;
using System;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|Parent
  /// </summary>
  public sealed class KeywordM : TreeItem, IEquatable<KeywordM>, IFilterItem, IRecord {
    #region IEquatable implementation
    public bool Equals(KeywordM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as KeywordM);
    public override int GetHashCode() => Id;
    public static bool operator ==(KeywordM a, KeywordM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(KeywordM a, KeywordM b) => !(a == b);
    #endregion

    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion
    
    public int Id { get; }
    public string FullName => Tree.GetFullName(this, "/", x => x.Name);

    public KeywordM() { }

    public KeywordM(int id, string name, ITreeItem parent) : base(Res.IconTag, name) {
      Id = id;
      Name = name;
      Parent = parent;
    }
  }
}
