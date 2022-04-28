using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|Segments|Keywords
  /// </summary>
  public sealed class PersonM : TreeItem, IEquatable<PersonM>, IRecord, IFilterItem {
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

    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion

    private SegmentM _segment;
    private ObservableCollection<KeywordM> _displayKeywords;

    public SegmentM Segment { get => _segment; set { _segment = value; OnPropertyChanged(); } }
    public ObservableCollection<object> TopSegments { get; set; }
    public List<KeywordM> Keywords { get; set; }
    public ObservableCollection<KeywordM> DisplayKeywords { get => _displayKeywords; set { _displayKeywords = value; OnPropertyChanged(); } }

    public PersonM() { }

    public PersonM(int id, string name) : base(Res.IconPeople, name) {
      Id = id;
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
