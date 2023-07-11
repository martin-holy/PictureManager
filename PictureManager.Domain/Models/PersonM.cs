using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|Segments|Keywords
  /// </summary>
  public sealed class PersonM : TreeItem, IEquatable<PersonM> {
    #region IEquatable implementation
    public bool Equals(PersonM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as PersonM);
    public override int GetHashCode() => Id;
    public static bool operator ==(PersonM a, PersonM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(PersonM a, PersonM b) => !(a == b);
    #endregion

    private SegmentM _segment;

    public int Id { get; }
    public SegmentM Segment { get => _segment; set { _segment = value; OnPropertyChanged(); } }
    public ObservableCollection<object> TopSegments { get; set; }
    public List<KeywordM> Keywords { get; set; }
    public IEnumerable<KeywordM> DisplayKeywords => KeywordsM.GetAllKeywords(Keywords);

    public PersonM() { }

    public PersonM(int id, string name) : base(Res.IconPeople, name) {
      Id = id;
    }

    public void UpdateDisplayKeywords() {
      OnPropertyChanged(nameof(Keywords));
      OnPropertyChanged(nameof(DisplayKeywords));
    }
  }
}
