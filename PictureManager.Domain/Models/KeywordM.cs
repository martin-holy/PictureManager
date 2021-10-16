using System;
using SimpleDB;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Interfaces;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|Parent
  /// </summary>
  public sealed class KeywordM : ObservableObject, IEquatable<KeywordM>, IRecord, ITreeBranch {
    #region IEquatable implementation
    public bool Equals(KeywordM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as KeywordM);
    public override int GetHashCode() => Id;
    public static bool operator ==(KeywordM a, KeywordM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(KeywordM a, KeywordM b) => !(a == b);
    #endregion

    #region IRecord implementation
    public int Id { get; }
    public string[] Csv { get; set; }
    #endregion

    #region ITreeBranch implementation
    public object Parent { get; set; }
    public ObservableCollection<object> Items { get; set; } = new();
    #endregion
    
    private string _name;
    
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public string FullName => GetFullName();

    public KeywordM() { }

    public KeywordM(int id, string name, ITreeBranch parent) {
      Id = id;
      Name = name;
      Parent = parent;
    }

    private string GetFullName() {
      var keywords = new List<KeywordM>();
      Utils.Tree.GetThisAndParentRecursive(this, ref keywords);
      keywords.Reverse();
      return string.Join("/", keywords.Select(x => x.Name));
    }
  }
}
