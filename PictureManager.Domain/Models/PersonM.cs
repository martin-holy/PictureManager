using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|Segments|Keywords
  /// </summary>
  public sealed class PersonM : TreeItem, IEquatable<PersonM>, IRecord {
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

    public TreeWrapGroup GetSegments(IEnumerable<SegmentM> source, List<SegmentM> allSegments) {
      allSegments.Clear();

      var root = new TreeWrapGroup() { IsExpanded = true };
      root.Info.Add(new(Res.IconImage, "Segments"));

      var groupedByKeywords = source
        .Where(x => x.Person == this)
        .GroupBy(x => string.Join(", ", KeywordsM.GetAllKeywords(x.Keywords).Select(k => k.Name)))
        .OrderBy(x => x.Key)
        .ToArray();

      if (groupedByKeywords.Length == 0) return root;

      if (groupedByKeywords.Length == 1) {
        foreach (var segment in groupedByKeywords[0].OrderBy(x => x.MediaItem.FileName)) {
          allSegments.Add(segment);
          root.Items.Add(segment);
        }

        root.Info.Add(new(Res.IconImageMultiple, root.Items.Count.ToString()));

        return root;
      }

      foreach (var group in groupedByKeywords) {
        var kGroup = new TreeWrapGroup() { IsExpanded = true };
        root.Items.Add(kGroup);

        if (!string.IsNullOrEmpty(group.Key))
          kGroup.Info.Add(new(Res.IconTag, group.Key));

        foreach (var segment in group.OrderBy(x => x.MediaItem.FileName)) {
          allSegments.Add(segment);
          kGroup.Items.Add(segment);
        }

        kGroup.Info.Add(new(Res.IconImageMultiple, kGroup.Items.Count.ToString()));
      }

      root.Info.Add(new(Res.IconImageMultiple, allSegments.Count.ToString()));

      return root;
    }
  }
}
