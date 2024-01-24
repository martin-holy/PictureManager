using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Models;

public sealed class SegmentM : ObservableObject, IEquatable<SegmentM>, ISelectable, IHaveKeywords {
  private bool _isSelected;

  #region DB Properties
  private PersonM _person;
  private double _x;
  private double _y;
  private double _size;

  public int Id { get; }
  public MediaItemM MediaItem { get; set; }
  public PersonM Person { get => _person; set { _person = value; OnPropertyChanged(); } }
  public List<KeywordM> Keywords { get; set; }

  public double X {
    get => _x;
    set {
      _x = value;

      // bounds check
      if (MediaItem != null) {
        if (value < 0) _x = 0;
        if (value > MediaItem.Width - Size) _x = MediaItem.Width - Size;
      }

      OnPropertyChanged();
    }
  }

  public double Y {
    get => _y;
    set {
      _y = value;

      // bounds check
      if (MediaItem != null) {
        if (value < 0) _y = 0;
        if (value > MediaItem.Height - Size) _y = MediaItem.Height - Size;
      }

      OnPropertyChanged();
    }
  }

  public double Size {
    get => _size;
    set {
      _size = value;

      // bounds check
      if (MediaItem != null) {
        var max = Math.Min(MediaItem.Width, MediaItem.Height);
        _size = value > max ? max : value;
      }

      OnPropertyChanged();
    }
  }
  #endregion DB Properties

  public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
  public string FilePathCache => IOExtensions.PathCombine(Path.GetDirectoryName(MediaItem.FilePathCache), $"segment_{GetHashCode().ToString()}.jpg");

  public SegmentM() { }

  public SegmentM(int id, double x, double y, double size) {
    Id = id;
    X = x;
    Y = y;
    Size = size;
  }

  #region IEquatable implementation
  public bool Equals(SegmentM other) => Id == other?.Id;
  public override bool Equals(object obj) => Equals(obj as SegmentM);
  public override int GetHashCode() => Id;
  public static bool operator ==(SegmentM a, SegmentM b) => a?.Equals(b) ?? b is null;
  public static bool operator !=(SegmentM a, SegmentM b) => !(a == b);
  #endregion IEquatable implementation

  public string ToMsRect() =>
    string.Join(", ",
      Math.Round(X / MediaItem.Width, 6).ToString(CultureInfo.InvariantCulture),
      Math.Round(Y / MediaItem.Height, 6).ToString(CultureInfo.InvariantCulture),
      Math.Round(Size / MediaItem.Width, 6).ToString(CultureInfo.InvariantCulture),
      Math.Round(Size / MediaItem.Height, 6).ToString(CultureInfo.InvariantCulture));
}

public static class SegmentExtensions {
  public static IEnumerable<FolderM> GetFolders(this IEnumerable<SegmentM> segments) =>
    segments
      .GetMediaItems()
      .GetFolders();

  public static IEnumerable<GeoNameM> GetGeoNames(this IEnumerable<SegmentM> segments) =>
    segments
      .GetMediaItems()
      .GetGeoNames();

  public static IEnumerable<KeywordM> GetKeywords(this IEnumerable<SegmentM> segments) =>
    segments
      .EmptyIfNull()
      .Where(x => x.Keywords != null)
      .SelectMany(x => x.Keywords)
      .Distinct()
      .SelectMany(Tree.GetThisAndParents)
      .Distinct();

  public static IEnumerable<MediaItemM> GetMediaItems(this IEnumerable<SegmentM> segments) =>
    segments
      .EmptyIfNull()
      .Select(x => x.MediaItem)
      .Distinct();

  public static IEnumerable<PersonM> GetPeople(this IEnumerable<SegmentM> segments) =>
    segments
      .EmptyIfNull()
      .Where(x => x.Person != null)
      .Select(x => x.Person)
      .Distinct();
}