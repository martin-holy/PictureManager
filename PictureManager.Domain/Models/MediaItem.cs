using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Utils;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public class MediaItem : INotifyPropertyChanged, IRecord, IEquatable<MediaItem> {
    public string[] Csv { get; set; }

    // DB Fields
    public int Id { get; }
    public Folder Folder { get; set; }
    public string FileName { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Orientation { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; }
    public GeoName GeoName { get; set; }
    public List<Person> People { get; set; }
    public List<Keyword> Keywords { get; set; }

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
    public ObservableCollection<VideoClip> VideoClips { get; set; }
    public ObservableCollection<VideoClipsGroup> VideoClipsGroups { get; set; }

    public string FilePath => Extensions.PathCombine(Folder.FullPath, FileName);
    public string FilePathCache => FilePath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Instance.CachePath);
    public Uri FilePathUri => new Uri(FilePath);
    public Uri FilePathCacheUri => new Uri(FilePathCache);
    public int ThumbSize { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public bool IsModified { get; set; }
    public bool IsNew { get; set; }
    public bool IsPanoramic { get; set; }
    public bool IsOnlyInDb { get; set; } // used when metadata can't be read/write
    public bool HasVideoClips => VideoClips?.Count > 0 || VideoClipsGroups?.Count > 0;

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public MediaItem(int id, Folder folder, string fileName, bool isNew = false) {
      Id = id;
      Folder = folder;
      FileName = fileName;
      IsNew = isNew;
      MediaType = Imaging.GetMediaType(fileName);
    }

    #region IEquatable implementation

    public bool Equals(MediaItem other) {
      return Id == other?.Id;
    }

    public override bool Equals(object obj) {
      return Equals(obj as MediaItem);
    }

    public override int GetHashCode() {
      return Id;
    }

    public static bool operator ==(MediaItem mi1, MediaItem mi2) {
      return mi1?.Equals(mi2) ?? ReferenceEquals(mi2, null);
    }

    public static bool operator !=(MediaItem mi1, MediaItem mi2) {
      return !(mi1 == mi2);
    }

    #endregion

    public string ToCsv() {
      // ID|Folder|Name|Width|Height|Orientation|Rating|Comment|GeoName|People|Keywords|IsOnlyInDb
      return string.Join("|",
        Id.ToString(),
        Folder.Id.ToString(),
        FileName,
        Width.ToString(),
        Height.ToString(),
        Orientation.ToString(),
        Rating.ToString(),
        Comment ?? string.Empty,
        GeoName?.Id.ToString(),
        People == null ? string.Empty : string.Join(",", People.Select(x => x.Id)),
        Keywords == null ? string.Empty : string.Join(",", Keywords.Select(x => x.Id)),
        IsOnlyInDb ? "1" : string.Empty);
    }

    public int RotationAngle {
      get {
        switch ((MediaOrientation) Orientation) {
          case MediaOrientation.Rotate90: return 90;
          case MediaOrientation.Rotate180: return 180;
          case MediaOrientation.Rotate270: return 270;
        }

        return 0;
      }
    }

    public void SetThumbSize(bool reload = false) {
      if (ThumbSize != 0 && !reload) return;
      if (Width == 0 || Height == 0) return;

      // TODO: move next and last line calculation elsewhere
      var desiredSize = (int)(Core.Instance.ThumbnailSize / Core.Instance.WindowsDisplayScale * 100 * Core.Instance.ThumbScale);
      var rotated = Orientation == (int) MediaOrientation.Rotate90 || Orientation == (int) MediaOrientation.Rotate270;
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
        InfoBoxThumb.Add(GeoName.Title);

      if (People != null) {
        InfoBoxPeople = new ObservableCollection<string>();

        foreach (var p in People.OrderBy(x => x.Title)) {
          InfoBoxPeople.Add(p.Title);
          InfoBoxThumb.Add(p.Title);
        }
      }

      if (Keywords != null) {
        InfoBoxKeywords = new ObservableCollection<string>();
        var allKeywords = new List<ICatTreeViewItem>();

        foreach (var keyword in Keywords)
          CatTreeViewUtils.GetThisAndParentRecursive(keyword, ref allKeywords);

        foreach (var keyword in allKeywords.OfType<Keyword>().Distinct().OrderBy(x => x.FullPath)) {
          InfoBoxKeywords.Add(keyword.Title);
          InfoBoxThumb.Add(keyword.Title);
        }
      }

      if (InfoBoxThumb.Count == 0)
        InfoBoxThumb = null;

      OnPropertyChanged(nameof(InfoBoxThumb));
      OnPropertyChanged(nameof(InfoBoxPeople));
      OnPropertyChanged(nameof(InfoBoxKeywords));
    }

    public MediaItem CopyTo(Folder folder, string fileName) {
      var copy = new MediaItem(Core.Instance.MediaItems.Helper.GetNextId(), folder, fileName) {
        Width = Width,
        Height = Height,
        Orientation = Orientation,
        Rating = Rating,
        Comment = Comment,
        GeoName = GeoName,
        Lat = Lat,
        Lng = Lng
      };

      if (People != null) {
        copy.People = new List<Person>(People);
        copy.People.ForEach(x => x.MediaItems.Add(copy));
      }

      if (Keywords != null) {
        copy.Keywords = new List<Keyword>(Keywords);
        copy.Keywords.ForEach(x => x.MediaItems.Add(copy));
      }

      copy.Folder.MediaItems.Add(copy);
      copy.GeoName?.MediaItems.Add(copy);
      
      copy.SetThumbSize();
      copy.SetInfoBox();

      Core.Instance.MediaItems.All.Add(copy);
      Core.Instance.MediaItems.MediaItemsCount++;

      return copy;
    }

    public void MoveTo(Folder folder, string fileName) {
      // delete existing MediaItem if exists
      Core.Instance.MediaItems.Delete(folder.MediaItems.SingleOrDefault(x => x.FileName.Equals(fileName)));

      FileName = fileName;
      Folder.MediaItems.Remove(this);
      Folder = folder;
      Folder.MediaItems.Add(this);
    }

    public void Rename(string newFileName) {
      Core.Instance.Sdb.SetModified<MediaItems>();
      var oldFilePath = FilePath;
      var oldFilePathCache = FilePathCache;
      FileName = newFileName;
      File.Move(oldFilePath, FilePath);
      File.Move(oldFilePathCache, FilePathCache);
    }

    public void ReloadThumbnail() {
      OnPropertyChanged(nameof(FilePathCacheUri));
    }

    public static string GetDateTimeFromName(MediaItem mi, string format) {
      if (mi == null) return string.Empty;

      var sdt = mi.FileName.Length < 15 ? string.Empty : mi.FileName.Substring(0, 15);
      var success = DateTime.TryParseExact(sdt, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt);

      return success ? dt.ToString(format, CultureInfo.CurrentCulture) : string.Empty;
    }

    public VideoClipsGroup VideoClipsGroupAdd(VideoClipsGroup group) {
      if (VideoClipsGroups == null)
        VideoClipsGroups = new ObservableCollection<VideoClipsGroup>();
      VideoClipsGroups.Add(group);

      return group;
    }

    public VideoClip VideoClipAdd(VideoClip vc, VideoClipsGroup group) {
      if (group == null) {
        if (VideoClips == null)
          VideoClips = new ObservableCollection<VideoClip>();
        VideoClips.Add(vc);
      }
      else {
        group.Clips.Add(vc);
        vc.Group = group;
      }

      return vc;
    }
  }
}
