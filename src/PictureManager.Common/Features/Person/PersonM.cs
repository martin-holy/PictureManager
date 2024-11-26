using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.Segment;
using BitsMasks = PictureManager.Common.Utils.BitsMasks;

namespace PictureManager.Common.Features.Person;

/// <summary>
/// DB fields: ID|Name|Segment|Keywords
/// </summary>
public sealed class PersonM : TreeItem, IEquatable<PersonM>, IHaveKeywords {
  #region IEquatable implementation
  public bool Equals(PersonM? other) => Id == other?.Id;
  public override bool Equals(object? obj) => Equals(obj as PersonM);
  public override int GetHashCode() => Id;
  public static bool operator ==(PersonM? a, PersonM? b) {
    if (ReferenceEquals(a, b)) return true;
    if (a is null || b is null) return false;
    return a.Equals(b);
  }
  public static bool operator !=(PersonM? a, PersonM? b) => !(a == b);
  #endregion

  private SegmentM? _segment;

  public int Id { get; }
  public SegmentM? Segment { get => _segment; set { _segment = value; OnPropertyChanged(); } }
  public ExtObservableCollection<SegmentM>? TopSegments { get; set; }
  public List<KeywordM>? Keywords { get; set; }
  public KeywordM[] DisplayKeywords => Keywords!.GetKeywords().OrderBy(x => x.FullName).ToArray();
  public bool IsUnknown { get => _bits[BitsMasks.IsUnknown]; set { _bits[BitsMasks.IsUnknown] = value; OnPropertyChanged(); } }
  public List<SegmentM>? Segments { get; set; }

  public PersonM() { }

  public PersonM(int id, string name) : base(Res.IconPeople, name) {
    Id = id;
  }

  public void ToggleTopSegment(SegmentM segment) {
    var flag = TopSegments == null;
    TopSegments = TopSegments.Toggle(segment, true);
    if (flag) OnPropertyChanged(nameof(TopSegments));
  }
}

public static class PersonExtensions {
  public static IEnumerable<FolderM> GetFolders(this PersonM person) =>
    person.Segments.EmptyIfNull().GetFolders();

  public static IEnumerable<FolderM> GetFolders(this IEnumerable<PersonM> people) =>
    people.SelectMany(GetFolders).Distinct();
}