using MH.Utils.BaseClasses;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Models;

/// <summary>
/// DB fields: ID|Name|Segment|Keywords
/// </summary>
public sealed class PersonM : TreeItem, IEquatable<PersonM>, IHaveKeywords {
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
  public IEnumerable<KeywordM> DisplayKeywords => Keywords.GetKeywords().OrderBy(x => x.FullName).ToArray();
  public bool IsUnknown { get => Bits[BitsMasks.IsUnknown]; set { Bits[BitsMasks.IsUnknown] = value; OnPropertyChanged(); } }

  public PersonM() { }

  public PersonM(int id, string name) : base(Res.IconPeople, name) {
    Id = id;
  }

  public void UpdateDisplayKeywords() {
    OnPropertyChanged(nameof(Keywords));
    OnPropertyChanged(nameof(DisplayKeywords));
  }
}