using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.DataViews;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models.MediaItems;

public abstract class MediaItemM : ObservableObject, ISelectable, IEquatable<MediaItemM>, IHaveKeywords {
  #region IEquatable implementation
  public bool Equals(MediaItemM other) => Id == other?.Id;
  public override bool Equals(object obj) => Equals(obj as MediaItemM);
  public override int GetHashCode() => Id;
  public static bool operator ==(MediaItemM a, MediaItemM b) => a?.Equals(b) ?? b is null;
  public static bool operator !=(MediaItemM a, MediaItemM b) => !(a == b);
  #endregion

  #region ISelectable implementation
  private bool _isSelected;
  public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
  #endregion

  public int Id { get; }
  public int Rating { get; set; }
  public string Comment { get; set; }
  public GeoLocationM GeoLocation { get; set; }
  public List<PersonM> People { get; set; }
  public List<KeywordM> Keywords { get; set; }
  public List<SegmentM> Segments { get; set; }

  public abstract FolderM Folder { get; set; }
  public abstract string FileName { get; set; }
  public abstract string FilePath { get; }
  public virtual string FilePathCache => throw new NotImplementedException();
  public abstract int Width { get; set; }
  public abstract int Height { get; set; }
  public abstract int ThumbWidth { get; set; }
  public abstract int ThumbHeight { get; set; }
  public abstract int Orientation { get; set; }

  public bool IsOnlyInDb { get; set; } // used when metadata can't be read/write

  public ExtObservableCollection<string> InfoBoxThumb { get; set; }

  public int RotationAngle =>
    (MediaOrientation)Orientation switch {
      MediaOrientation.Rotate90 => 90,
      MediaOrientation.Rotate180 => 180,
      MediaOrientation.Rotate270 => 270,
      _ => 0,
    };

  public PersonM[] DisplayPeople =>
    GetPeople().OrderBy(x => x.Name).ToArray().NullIfEmpty();

  public string[] DisplayKeywords =>
    Keywords?.ToStrings(x => x.Name).ToArray();

  protected MediaItemM(int id) {
    Id = id;
  }

  public IEnumerable<FolderM> GetFolders() =>
    Folder.GetThisAndParents();

  public IEnumerable<GeoNameM> GetGeoNames() =>
    GeoLocation?.GeoName?.GetThisAndParents() ?? Enumerable.Empty<GeoNameM>();

  public IEnumerable<KeywordM> GetKeywords() =>
    Keywords
      .EmptyIfNull()
      .Concat(GetSegments().GetKeywords())
      .Distinct()
      .SelectMany(x => x.GetThisAndParents())
      .Distinct();

  public IEnumerable<PersonM> GetPeople() =>
    People
      .EmptyIfNull()
      .Concat(Segments.GetPeople())
      .Distinct();

  public IEnumerable<SegmentM> GetSegments() =>
    Segments.EmptyIfNull();

  public void SetInfoBox() {
    InfoBoxThumb?.Clear();
    InfoBoxThumb = new();

    if (Rating != 0)
      InfoBoxThumb.Add(Rating.ToString());

    if (!string.IsNullOrEmpty(Comment))
      InfoBoxThumb.Add(Comment);

    if (GeoLocation?.GeoName != null)
      InfoBoxThumb.Add(GeoLocation.GeoName.Name);

    if (People != null || Segments != null)
      InfoBoxThumb.AddItems(GetPeople().Select(x => x.Name).OrderBy(x => x).ToArray(), null);

    if (Keywords != null)
      InfoBoxThumb.AddItems(DisplayKeywords, null);

    if (InfoBoxThumb.Count == 0)
      InfoBoxThumb = null;

    OnPropertyChanged(nameof(InfoBoxThumb));
  }

  public void SetThumbSize(bool reload = false) {
    if (ThumbWidth != 0 && ThumbHeight != 0 && !reload) return;
    if (Width == 0 || Height == 0) return;

    // TODO: move next and last line calculation elsewhere
    var desiredSize = (int)(Core.Settings.ThumbnailSize * MediaItemsViews.DefaultThumbScale);
    var rotated = Orientation is (int)MediaOrientation.Rotate90 or (int)MediaOrientation.Rotate270;

    // TODO move rotation check to GetThumbSize or create func for getting w & h rotated
    Imaging.GetThumbSize(
      rotated ? Height : Width,
      rotated ? Width : Height,
      desiredSize,
      out var w,
      out var h);

    ThumbWidth = w;
    ThumbHeight = h;
  }

  public bool IsPanoramic() =>
    Orientation is (int)MediaOrientation.Rotate90 or (int)MediaOrientation.Rotate270
      ? Height / (double)Width > 16.0 / 9.0
      : Width / (double)Height > 16.0 / 9.0;
}

public static class MediaItemExtensions {
  public static T GetByFileName<T>(this IEnumerable<T> items, string fileName) where T : MediaItemM =>
    items.SingleOrDefault(x => x.FileName.Equals(fileName, StringComparison.Ordinal));

  public static IEnumerable<FolderM> GetFolders(this IEnumerable<MediaItemM> items) =>
    items
      .EmptyIfNull()
      .SelectMany(x => x.GetFolders())
      .Distinct();

  public static IEnumerable<GeoNameM> GetGeoNames(this IEnumerable<MediaItemM> items) =>
    items
      .EmptyIfNull()
      .SelectMany(x => x.GetGeoNames())
      .Distinct();

  public static IEnumerable<KeywordM> GetKeywords(this IEnumerable<MediaItemM> items) =>
    items
      .EmptyIfNull()
      .SelectMany(x => x.GetKeywords())
      .Distinct();

  public static IEnumerable<PersonM> GetPeople(this IEnumerable<MediaItemM> items) =>
    items
      .EmptyIfNull()
      .SelectMany(x => x.GetPeople())
      .Distinct();

  public static IEnumerable<SegmentM> GetSegments(this IEnumerable<MediaItemM> items) =>
    items
      .EmptyIfNull()
      .SelectMany(x => x.GetSegments());
}