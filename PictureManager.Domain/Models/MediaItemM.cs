using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Utils;
using SimpleDB;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Folder|Name|Width|Height|Orientation|Rating|Comment|GeoName|People|Keywords|IsOnlyInDb
  /// </summary>
  public sealed class MediaItemM : ObservableObject, IEquatable<MediaItemM>, ISelectable, IRecord {
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
    public FolderM Folder { get; set; }
    public string FileName { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Orientation { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; }
    public GeoNameM GeoName { get; set; }
    public List<PersonM> People { get; set; }
    public List<KeywordM> Keywords { get; set; }
    public bool IsOnlyInDb { get; set; } // used when metadata can't be read/write

    private int _thumbWidth;
    private int _thumbHeight;
    private MediaType _mediaType;
    private bool _hasVideoClips;

    public int ThumbWidth { get => _thumbWidth; set { _thumbWidth = value; OnPropertyChanged(); } }
    public int ThumbHeight { get => _thumbHeight; set { _thumbHeight = value; OnPropertyChanged(); } }
    public int ThumbSize { get; set; }
    public MediaType MediaType { get => _mediaType; set { _mediaType = value; OnPropertyChanged(); } }
    public bool HasVideoClips { get => _hasVideoClips; set { _hasVideoClips = value; OnPropertyChanged(); } }
    public ObservableCollection<SegmentM> Segments { get; set; }
    public ObservableCollection<string> InfoBoxThumb { get; set; }
    public ObservableCollection<PersonM> InfoBoxPeople { get; set; }
    public ObservableCollection<string> InfoBoxKeywords { get; set; }
    public string FilePath => IOExtensions.PathCombine(Folder.FullPath, FileName);
    public string FilePathCache => FilePath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Instance.CachePath) +
                                   (MediaType == MediaType.Image ? string.Empty : ".jpg");
    public int RotationAngle =>
      (MediaOrientation)Orientation switch {
        MediaOrientation.Rotate90 => 90,
        MediaOrientation.Rotate180 => 180,
        MediaOrientation.Rotate270 => 270,
        _ => 0,
      };

    // TODO rethink
    public double? Lat { get; set; }
    public double? Lng { get; set; }

    // TODO maybe not needed
    public bool IsNew { get; set; }

    public bool IsPanoramic { get; set; }

    public MediaItemM(int id, FolderM folder, string fileName, bool isNew = false) {
      Id = id;
      Folder = folder;
      FileName = fileName;
      IsNew = isNew;
      MediaType = Imaging.GetMediaType(fileName);
    }

    public void SetThumbSize(bool reload = false) {

      if (ThumbSize != 0 && !reload) return;
      if (Width == 0 || Height == 0) return;

      // TODO pass core as parameter
      var core = Core.Instance;
      // TODO pass scale as parameter
      var thumbScale = core.ThumbnailsGridsM.Current?.ThumbScale
                       ?? core.ThumbnailsGridsM.DefaultThumbScale;

      // TODO: move next and last line calculation elsewhere
      var desiredSize = (int)(core.ThumbnailSize * thumbScale);
      var rotated = Orientation is (int)MediaOrientation.Rotate90 or (int)MediaOrientation.Rotate270;
      
      // TODO move rotation check to GetThumbSize or create func for getting w & h rotated
      Imaging.GetThumbSize(
        rotated ? Height : Width,
        rotated ? Width : Height,
        desiredSize,
        out var w,
        out var h);

      IsPanoramic = w > desiredSize;
      ThumbWidth = w;
      ThumbHeight = h;
      ThumbSize = (int)((w > h ? w : h) / thumbScale);
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

      if (GeoName != null)
        InfoBoxThumb.Add(GeoName.Name);

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
        var allKeywords = new List<KeywordM>();

        foreach (var keyword in Keywords)
          MH.Utils.Tree.GetThisAndParentRecursive(keyword, ref allKeywords);

        foreach (var keyword in allKeywords.Distinct().OrderBy(x => x.FullName)) {
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
  }
}
