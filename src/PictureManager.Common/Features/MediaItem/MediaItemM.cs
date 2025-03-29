﻿using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.GeoLocation;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.MediaItem.Video;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Common.Features.MediaItem;

public abstract class MediaItemM(int id) : ObservableObject, ISelectable, IEquatable<MediaItemM>, IHaveKeywords {
  #region IEquatable implementation
  public bool Equals(MediaItemM? other) => Id == other?.Id;
  public override bool Equals(object? obj) => Equals(obj as MediaItemM);
  public override int GetHashCode() => Id;
  public static bool operator ==(MediaItemM? a, MediaItemM? b) {
    if (ReferenceEquals(a, b)) return true;
    if (a is null || b is null) return false;
    return a.Equals(b);
  }
  public static bool operator !=(MediaItemM? a, MediaItemM? b) => !(a == b);
  #endregion

  #region ISelectable implementation
  private bool _isSelected;
  public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
  #endregion

  public int Id { get; } = id;
  public int Rating { get; set; }
  public string? Comment { get; set; }
  public GeoLocationM? GeoLocation { get; set; }
  public List<PersonM>? People { get; set; }
  public List<KeywordM>? Keywords { get; set; }
  public List<SegmentM>? Segments { get; set; }

  public abstract FolderM Folder { get; set; }
  public abstract string FileName { get; set; }
  public virtual string FilePath => IOExtensions.PathCombine(Folder.FullPath, FileName);
  public virtual string FileNameCache(string fileName) => fileName;
  public virtual string FilePathCache => Core.P.GetFilePathCache(Folder, FileNameCache(FileName));
  public abstract int Width { get; set; }
  public abstract int Height { get; set; }
  public abstract int ThumbWidth { get; set; }
  public abstract int ThumbHeight { get; set; }
  public abstract Orientation Orientation { get; set; }

  public ExtObservableCollection<string>? InfoBoxThumb { get; set; }

  public PersonM[]? DisplayPeople =>
    GetPeople().OrderBy(x => x.Name).ToArray().NullIfEmpty();

  public string[]? DisplayKeywords =>
    GetKeywords().ToStrings(x => x.Name).ToArray().NullIfEmpty();

  public IEnumerable<FolderM> GetFolders() =>
    Folder.GetThisAndParents();

  public IEnumerable<GeoNameM> GetGeoNames() =>
    GeoLocation?.GeoName?.GetThisAndParents() ?? [];

  public virtual IEnumerable<KeywordM> GetKeywords() =>
    Keywords!.GetKeywords();

  public virtual IEnumerable<PersonM> GetPeople() =>
    People
      .EmptyIfNull()
      .Concat(Segments.EmptyIfNull().GetPeople())
      .Distinct();

  public IEnumerable<SegmentM> GetSegments() =>
    Segments.EmptyIfNull();

  public virtual void SetInfoBox(bool update = false) {
    if (InfoBoxThumb != null && !update) return;
    InfoBoxThumb?.Clear();
    InfoBoxThumb = [];

    if (Rating != 0)
      InfoBoxThumb.Add(Rating.ToString());

    if (!string.IsNullOrEmpty(Comment))
      InfoBoxThumb.Add(Comment);

    if (GeoLocation?.GeoName != null)
      InfoBoxThumb.Add(GeoLocation.GeoName.Name);

    InfoBoxThumb.AddItems(DisplayPeople.EmptyIfNull().Select(x => x.Name).ToArray(), null);
    InfoBoxThumb.AddItems(DisplayKeywords, null);

    if (InfoBoxThumb.Count == 0)
      InfoBoxThumb = null;

    OnPropertyChanged(nameof(DisplayKeywords));
    OnPropertyChanged(nameof(DisplayPeople));
    OnPropertyChanged(nameof(InfoBoxThumb));
  }

  public void SetThumbSize(bool reload = false) {
    if (ThumbWidth != 0 && ThumbHeight != 0 && !reload) return;
    if (Width == 0 || Height == 0) return;

    var rotated = Orientation is Orientation.Rotate90 or Orientation.Rotate270;
    Imaging.GetThumbSize(
      rotated ? Height : Width,
      rotated ? Width : Height,
      Core.Settings.MediaItem.ThumbSize,
      out var w,
      out var h);

    ThumbWidth = w;
    ThumbHeight = h;
  }
}

public static class MediaItemExtensions {
  public static T? GetByFileName<T>(this IEnumerable<T> items, string fileName) where T : MediaItemM =>
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

  public static IEnumerable<PersonM> GetPeople(this IEnumerable<MediaItemM> items) =>
    items
      .EmptyIfNull()
      .SelectMany(x => x.GetPeople())
      .Distinct();

  public static IEnumerable<SegmentM> GetSegments(this IEnumerable<MediaItemM> items) =>
    items
      .EmptyIfNull()
      .SelectMany(x => x.GetSegments());

  public static IEnumerable<VideoItemM> GetVideoItems(this MediaItemM[] items) =>
    items
      .OfType<VideoM>()
      .SelectMany(x => x.GetVideoItems())
      .Concat(items.OfType<VideoItemM>())
      .Distinct();
}