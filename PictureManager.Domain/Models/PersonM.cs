using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|Segments|Keywords
  /// </summary>
  public sealed class PersonM : ObservableObject, IEquatable<PersonM>, IRecord, ITreeLeaf, ISelectable {
    #region IEquatable implementation
    public bool Equals(PersonM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as PersonM);
    public override int GetHashCode() => Id;
    public static bool operator ==(PersonM a, PersonM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(PersonM a, PersonM b) => !(a == b);
    #endregion

    #region IRecord implementation
    public int Id { get; }
    public string[] Csv { get; set; }
    #endregion

    #region ITreeLeaf implementation
    public ITreeBranch Parent { get; set; }
    #endregion

    #region ISelectable implementation
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    #endregion

    private string _name;
    private SegmentM _segment;
    private ObservableCollection<KeywordM> _displayKeywords;

    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public SegmentM Segment { get => _segment; set { _segment = value; OnPropertyChanged(); } }
    public List<SegmentM> TopSegments { get; set; }
    public List<KeywordM> Keywords { get; set; }
    public ObservableCollection<KeywordM> DisplayKeywords { get => _displayKeywords; set { _displayKeywords = value; OnPropertyChanged(); } }

    public PersonM() { }

    public PersonM(int id, string name) {
      Id = id;
      Name = name;
    }

    public void UpdateDisplayKeywords() {
      DisplayKeywords?.Clear();

      if (Keywords == null) {
        if (DisplayKeywords != null)
          DisplayKeywords = null;

        return;
      }

      DisplayKeywords ??= new();
      var allKeywords = new List<KeywordM>();

      foreach (var keyword in Keywords)
        MH.Utils.Tree.GetThisAndItemsRecursive(keyword, ref allKeywords);

      foreach (var keyword in allKeywords.Distinct().OrderBy(x => x.FullName))
        DisplayKeywords.Add(keyword);
    }
  }
}
