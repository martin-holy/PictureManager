using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.DataViews;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Models.MediaItems;

public abstract class MediaItemM : ObservableObject, ISelectable, IEquatable<MediaItemM> {
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
  public List<PersonM> People { get; set; }
  public List<KeywordM> Keywords { get; set; }
  public ObservableCollection<SegmentM> Segments { get; set; }

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

  public ObservableCollection<string> InfoBoxThumb { get; set; }
  public ObservableCollection<PersonM> InfoBoxPeople { get; set; }
  public ObservableCollection<string> InfoBoxKeywords { get; set; }

  public int RotationAngle =>
    (MediaOrientation)Orientation switch {
      MediaOrientation.Rotate90 => 90,
      MediaOrientation.Rotate180 => 180,
      MediaOrientation.Rotate270 => 270,
      _ => 0,
    };

  protected MediaItemM(int id) {
    Id = id;
  }

  // TODO update just when needed
  public void SetInfoBox() {
    InfoBoxPeople?.Clear();
    InfoBoxPeople = null;
    InfoBoxKeywords?.Clear();
    InfoBoxKeywords = null;
    InfoBoxThumb?.Clear();
    InfoBoxThumb = new();

    if (Rating != 0)
      InfoBoxThumb.Add(Rating.ToString());

    if (!string.IsNullOrEmpty(Comment))
      InfoBoxThumb.Add(Comment);

    var g = Core.Db.MediaItemGeoLocation.GetBy(this)?.GeoName;
    if (g != null)
      InfoBoxThumb.Add(g.Name);

    if (People != null || Segments != null) {
      var people = (
          People == null
            ? Array.Empty<PersonM>()
            : People.ToArray())
        .Concat(
          Segments == null
            ? Array.Empty<PersonM>()
            : Segments
              .Where(x => x.Person != null)
              .Select(x => x.Person))
        .ToArray();

      if (people.Any()) {
        InfoBoxPeople = new();

        foreach (var p in people.Distinct().OrderBy(x => x.Name)) {
          InfoBoxPeople.Add(p);
          InfoBoxThumb.Add(p.Name);
        }
      }
    }

    if (Keywords != null) {
      InfoBoxKeywords = new();
      var keywords = Keywords
        .SelectMany(x => x.GetThisAndParents())
        .Distinct()
        .OrderBy(x => x.FullName);

      foreach (var keyword in keywords) {
        InfoBoxKeywords.Add(keyword.Name);
        InfoBoxThumb.Add(keyword.Name);
      }
    }

    if (InfoBoxThumb.Count == 0)
      InfoBoxThumb = null;

    OnPropertyChanged(nameof(InfoBoxThumb));
    OnPropertyChanged(nameof(InfoBoxPeople));
    OnPropertyChanged(nameof(InfoBoxKeywords));
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
}