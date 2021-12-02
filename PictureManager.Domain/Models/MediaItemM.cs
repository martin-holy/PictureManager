using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Utils;
using SimpleDB;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Folder|Name|Width|Height|Orientation|Rating|Comment|GeoName|People|Keywords|IsOnlyInDb
  /// </summary>
  public sealed class MediaItemM : ObservableObject, IEquatable<MediaItemM>, IRecord, ISelectable {
    #region IEquatable implementation
    public bool Equals(MediaItemM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as MediaItemM);
    public override int GetHashCode() => Id;
    public static bool operator ==(MediaItemM a, MediaItemM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(MediaItemM a, MediaItemM b) => !(a == b);
    #endregion

    #region IRecord implementation
    public int Id { get; }
    public string[] Csv { get; set; }
    #endregion

    #region ISelectable implementation
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    #endregion

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

    public int ThumbWidth { get => _thumbWidth; set { _thumbWidth = value; OnPropertyChanged(); } }
    public int ThumbHeight { get => _thumbHeight; set { _thumbHeight = value; OnPropertyChanged(); } }
    public int ThumbSize { get; set; }
    public MediaType MediaType { get => _mediaType; set { _mediaType = value; OnPropertyChanged(); } }
    public ObservableCollection<Segment> Segments { get; set; }
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


    // TODO move this to VideoClips
    public ObservableCollection<VideoClipM> VideoClips { get; set; }
    public ObservableCollection<VideoClipsGroupM> VideoClipsGroups { get; set; }
    public bool HasVideoClips => VideoClips?.Count > 0 || VideoClipsGroups?.Count > 0;

    // TODO rethink
    public double? Lat { get; set; }
    public double? Lng { get; set; }

    // TODO maybe not needed
    public bool IsNew { get; set; }

    // TODO move it to VM
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
      var thumbScale = core.ThumbnailsGridsM.Current?.ThumbScale ?? 1.0;

      // TODO: move next and last line calculation elsewhere
      var desiredSize = (int)(core.ThumbnailSize / core.WindowsDisplayScale * 100 * thumbScale);
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
      ThumbSize = (int)((w > h ? w : h) * core.WindowsDisplayScale / 100 / thumbScale);
    }

    public void ReloadThumbnail() => OnPropertyChanged(nameof(FilePathCache));
  }
}
