using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using MH.Utils;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Utils;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class MediaItem : INotifyPropertyChanged, IRecord, IEquatable<MediaItem>, ISelectable {
    public string[] Csv { get; set; }

    // DB Fields
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

    private bool _isSelected;
    private int _thumbWidth;
    private int _thumbHeight;
    private MediaType _mediaType;

    public int ThumbWidth { get => _thumbWidth; set { _thumbWidth = value; OnPropertyChanged(); } }
    public int ThumbHeight { get => _thumbHeight; set { _thumbHeight = value; OnPropertyChanged(); } }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public MediaType MediaType { get => _mediaType; set { _mediaType = value; OnPropertyChanged(); } }
    public ObservableCollection<string> InfoBoxThumb { get; set; }
    public ObservableCollection<string> InfoBoxPeople { get; set; }
    public ObservableCollection<string> InfoBoxKeywords { get; set; }
    public ObservableCollection<VideoClipM> VideoClips { get; set; }
    public ObservableCollection<VideoClipsGroupM> VideoClipsGroups { get; set; }
    public ObservableCollection<Segment> Segments { get; set; }
    public string Dimensions => $"{Width}x{Height}";
    public string FilePath => IOExtensions.PathCombine(Folder.FullPath, FileName);
    public string FilePathCache => FilePath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Instance.CachePath) +
                                   (MediaType == MediaType.Image ? string.Empty : ".jpg");
    public Uri FilePathUri => new(FilePath);
    public Uri FilePathCacheUri => new(FilePathCache);
    public int ThumbSize { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public bool IsModified { get; set; }
    public bool IsNew { get; set; }
    public bool IsPanoramic { get; set; }
    public bool IsOnlyInDb { get; set; } // used when metadata can't be read/write
    public bool HasVideoClips => VideoClips?.Count > 0 || VideoClipsGroups?.Count > 0;

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public MediaItem(int id, FolderM folder, string fileName, bool isNew = false) {
      Id = id;
      Folder = folder;
      FileName = fileName;
      IsNew = isNew;
      MediaType = Imaging.GetMediaType(fileName);
    }

    #region IEquatable implementation
    public bool Equals(MediaItem other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as MediaItem);
    public override int GetHashCode() => Id;
    public static bool operator ==(MediaItem a, MediaItem b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(MediaItem a, MediaItem b) => !(a == b);
    #endregion

    public int RotationAngle =>
      (MediaOrientation)Orientation switch {
        MediaOrientation.Rotate90 => 90,
        MediaOrientation.Rotate180 => 180,
        MediaOrientation.Rotate270 => 270,
        _ => 0,
      };

    public void SetThumbSize(bool reload = false) {
      if (ThumbSize != 0 && !reload) return;
      if (Width == 0 || Height == 0) return;

      // TODO: move next and last line calculation elsewhere
      var desiredSize = (int)(Core.Instance.ThumbnailSize / Core.Instance.WindowsDisplayScale * 100 * Core.Instance.ThumbScale);
      var rotated = Orientation is ((int)MediaOrientation.Rotate90) or ((int)MediaOrientation.Rotate270);
      Imaging.GetThumbSize(rotated ? Height : Width, rotated ? Width : Height, desiredSize, out _thumbWidth, out _thumbHeight);

      IsPanoramic = ThumbWidth > desiredSize;
      OnPropertyChanged(nameof(ThumbWidth));
      OnPropertyChanged(nameof(ThumbHeight));

      ThumbSize = (int)((ThumbWidth > ThumbHeight ? ThumbWidth : ThumbHeight) * Core.Instance.WindowsDisplayScale / 100 / Core.Instance.ThumbScale);
    }

    public void SetInfoBox() {
      InfoBoxPeople?.Clear();
      InfoBoxPeople = null;
      InfoBoxKeywords?.Clear();
      InfoBoxKeywords = null;
      InfoBoxThumb?.Clear();
      InfoBoxThumb = new ObservableCollection<string>();

      if (Rating != 0)
        InfoBoxThumb.Add(Rating.ToString());

      if (!string.IsNullOrEmpty(Comment))
        InfoBoxThumb.Add(Comment);

      if (GeoName != null)
        InfoBoxThumb.Add(GeoName.Name);

      if (People != null || Segments != null) {
        var people = Enumerable.Empty<string>();

        if (People != null)
          people = People.Select(x => x.Name);
        if (Segments != null)
          people = people.Concat(Segments.Where(x => x.Person != null).Select(x => x.Person.Name));

        if (people.Any()) {
          InfoBoxPeople = new ObservableCollection<string>();

          foreach (var p in people.Distinct().OrderBy(x => x)) {
            InfoBoxPeople.Add(p);
            InfoBoxThumb.Add(p);
          }
        }
      }

      if (Keywords != null) {
        InfoBoxKeywords = new ObservableCollection<string>();
        var allKeywords = new List<KeywordM>();

        foreach (var keyword in Keywords)
          Tree.GetThisAndParentRecursive(keyword, ref allKeywords);

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

    public MediaItem CopyTo(FolderM folder, string fileName) {
      var copy = new MediaItem(Core.Instance.MediaItems.DataAdapter.GetNextId(), folder, fileName) {
        Width = Width,
        Height = Height,
        Orientation = Orientation,
        Rating = Rating,
        Comment = Comment,
        GeoName = GeoName,
        Lat = Lat,
        Lng = Lng
      };

      if (People != null)
        copy.People = new(People);

      if (Keywords != null)
        copy.Keywords = new (Keywords);

      if (Segments != null) {
        copy.Segments = new();
        foreach (var segment in Segments) {
          var sCopy = Core.Instance.Segments.GetCopy(segment);
          sCopy.MediaItem = copy;
          copy.Segments.Add(sCopy);
        }
      }

      copy.Folder.MediaItems.Add(copy);

      copy.SetThumbSize();
      copy.SetInfoBox();

      Core.Instance.MediaItems.All.Add(copy);
      Core.Instance.MediaItems.MediaItemsCount++;

      return copy;
    }

    public void MoveTo(FolderM folder, string fileName) {
      // delete existing MediaItem if exists
      Core.Instance.MediaItems.Delete(folder.MediaItems.SingleOrDefault(x => x.FileName.Equals(fileName)));

      FileName = fileName;
      Folder.MediaItems.Remove(this);
      Folder = folder;
      Folder.MediaItems.Add(this);
    }

    public void Rename(string newFileName) {
      Core.Instance.MediaItems.DataAdapter.IsModified = true;
      var oldFilePath = FilePath;
      var oldFilePathCache = FilePathCache;
      FileName = newFileName;
      File.Move(oldFilePath, FilePath);
      File.Move(oldFilePathCache, FilePathCache);
    }

    public void ReloadThumbnail() => OnPropertyChanged(nameof(FilePathCacheUri));

    public static string GetDateTimeFromName(MediaItem mi, string format) {
      if (mi == null) return string.Empty;

      var sdt = mi.FileName.Length < 15 ? string.Empty : mi.FileName.Substring(0, 15);
      var success = DateTime.TryParseExact(sdt, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt);

      return success ? dt.ToString(format, CultureInfo.CurrentCulture) : string.Empty;
    }

    public VideoClipsGroupM VideoClipsGroupAdd(VideoClipsGroupM group) {
      VideoClipsGroups ??= new();
      VideoClipsGroups.Add(group);

      return group;
    }

    public VideoClipM VideoClipAdd(VideoClipM vc, VideoClipsGroupM group) {
      if (group == null) {
        VideoClips ??= new();
        VideoClips.Add(vc);
      }
      else {
        group.Clips.Add(vc);
        vc.Group = group;
      }

      OnPropertyChanged(nameof(HasVideoClips));

      return vc;
    }
  }
}
