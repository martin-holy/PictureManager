using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.GeoLocation;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Rating;
using PictureManager.Common.Features.Segment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaItemR : TableDataAdapter<MediaItemM> {
  public static MediaItemM Dummy { get; } = new ImageM(0, FolderR.Dummy, string.Empty);
  private readonly CoreR _coreR;
  private static readonly string[] _supportedImageExts = [".jpg", ".jpeg"];
  private static readonly string[] _supportedVideoExts = [".mp4"];

  public event EventHandler<MediaItemM> ItemRenamedEvent = delegate { };
  public event EventHandler<MediaItemM[]> MetadataChangedEvent = delegate { };
  public event EventHandler<RealMediaItemM[]> OrientationChangedEvent = delegate { };

  public MediaItemR(CoreR coreR) : base(coreR, string.Empty, 0) {
    _coreR = coreR;
    _coreR.ReadyEvent += _onDbReady;
  }

  private void _raiseItemRenamed(MediaItemM item) => ItemRenamedEvent(this, item);
  public void RaiseMetadataChanged(MediaItemM[] items) => MetadataChangedEvent(this, items);
  public void RaiseOrientationChanged(RealMediaItemM[] items) => OrientationChangedEvent(this, items);

  private void _onDbReady(object? sender, EventArgs e) {
    MaxId = _coreR.Image.MaxId;

    _coreR.Image.ItemCreatedEvent += OnItemCreated;
    _coreR.Image.ItemDeletedEvent += OnItemDeleted;
    _coreR.Image.ItemsDeletedEvent += _onMediaItemsDeleted;
    _coreR.Video.ItemCreatedEvent += OnItemCreated;
    _coreR.Video.ItemDeletedEvent += OnItemDeleted;
    _coreR.Video.ItemsDeletedEvent += _onMediaItemsDeleted;
    _coreR.VideoClip.ItemCreatedEvent += OnItemCreated;
    _coreR.VideoClip.ItemDeletedEvent += OnItemDeleted;
    _coreR.VideoClip.ItemsDeletedEvent += _onMediaItemsDeleted;
    _coreR.VideoImage.ItemCreatedEvent += OnItemCreated;
    _coreR.VideoImage.ItemDeletedEvent += OnItemDeleted;
    _coreR.VideoImage.ItemsDeletedEvent += _onMediaItemsDeleted;
  }

  protected override void OnItemCreated(object? sender, MediaItemM item) {
    if (item is RealMediaItemM rmi)
      rmi.Folder.MediaItems.Add(rmi);

    _raiseItemCreated(item);
  }

  protected override void OnItemDeleted(object? sender, MediaItemM item) =>
    _raiseItemDeleted(item);

  protected override void OnItemsDeleted(object? sender, IList<MediaItemM> items) =>
    _raiseItemsDeleted(items);

  private void _onMediaItemsDeleted<T>(object? sender, IList<T> items) where T : MediaItemM =>
    OnItemsDeleted(this, items.Cast<MediaItemM>().ToArray());

  public void OnItemDeletedCommon(MediaItemM item) {
    item.People = null;
    item.Keywords = null;
    item.Segments = null;

    if (item is not RealMediaItemM rmi) return;
    rmi.Folder.MediaItems.Remove(rmi);
  }

  public override int GetNextId() {
    var id = ++MaxId;
    _coreR.Image.MaxId = id;
    _coreR.Video.MaxId = id;
    _coreR.VideoClip.MaxId = id;
    _coreR.VideoImage.MaxId = id;
    return id;
  }

  public override MediaItemM? GetById(string id, bool nullable = false) {
    if (!int.TryParse(id, out var intId)) return null;
    if (_coreR.Image.AllDict.TryGetValue(intId, out var img)) return img;
    if (_coreR.Video.AllDict.TryGetValue(intId, out var vid)) return vid;
    if (_coreR.VideoClip.AllDict.TryGetValue(intId, out var vc)) return vc;
    if (_coreR.VideoImage.AllDict.TryGetValue(intId, out var vi)) return vi;
    return null;
  }

  public List<MediaItemM>? Link(string csv) {
    if (string.IsNullOrEmpty(csv)) return null;

    var items = csv
      .Split(',')
      .Select(x => GetById(x))
      .Where(x => x != null)
      .Select(x => x!)
      .ToList();

    return items.Count == 0 ? null : items;
  }

  public RealMediaItemM? ItemCreate(FolderM folder, string fileName) {
    if (_supportedImageExts.Any(x => fileName.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
      return _coreR.Image.ItemCreate(folder, fileName);

    if (_supportedVideoExts.Any(x => fileName.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
      return _coreR.Video.ItemCreate(folder, fileName);

    return null;
  }

  public void ItemRename(RealMediaItemM item, string newFileName) {
    var oldFilePath = item.FilePath;
    var oldFilePathCache = item.FilePathCache;
    item.FileName = newFileName;

    try {
      File.Move(oldFilePath, item.FilePath);
      File.Move(oldFilePathCache, item.FilePathCache);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
    
    ModifyOnlyDA(item);
    _raiseItemRenamed(item);
  }

  public void ModifyOnlyDA(RealMediaItemM mi) {
    switch (mi) {
      case ImageM img: _coreR.Image.Modify(img); break;
      case VideoM vid: _coreR.Video.Modify(vid); break;
    }
  }

  public override void Modify(MediaItemM mi) {
    switch (mi) {
      case ImageM img:
        _coreR.Image.Modify(img);
        img.IsOnlyInDb = true;
        break;
      case VideoM vid:
        _coreR.Video.Modify(vid);
        vid.IsOnlyInDb = true;
        break;
      case VideoClipM vc: _coreR.VideoClip.Modify(vc); break;
      case VideoImageM vi: _coreR.VideoImage.Modify(vi); break;
    }
  }

  public override void ItemDelete(MediaItemM mi, bool singleDelete = true) {
    switch (mi) {
      case ImageM img: _coreR.Image.ItemDelete(img); break;
      case VideoM vid: _coreR.Video.ItemDelete(vid); break;
      case VideoClipM vc: _coreR.VideoClip.ItemDelete(vc); break;
      case VideoImageM vi: _coreR.VideoImage.ItemDelete(vi); break;
    }
  }

  public override void ItemsDelete(IList<MediaItemM>? items) {
    if (items == null || items.Count == 0) return;
    _coreR.Image.ItemsDelete(items.OfType<ImageM>().ToArray());
    _coreR.Video.ItemsDelete(items.OfType<VideoM>().ToArray());
    _coreR.VideoClip.ItemsDelete(items.OfType<VideoClipM>().ToArray());
    _coreR.VideoImage.ItemsDelete(items.OfType<VideoImageM>().ToArray());
  }

  public void ItemsDeleteFromDrive(IList<MediaItemM> items) {
    try {
      CoreR.FileOperationDelete(items.OfType<RealMediaItemM>().Select(x => x.FilePath).Where(File.Exists).ToList(), true, false);
      items.Select(x => x.FilePathCache).Where(File.Exists).ToList().ForEach(File.Delete);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
    ItemsDelete(items);
  }

  public override IEnumerable<MediaItemM> GetAll(Func<MediaItemM, bool> where) =>
    _coreR.Image.All.Where(where)
      .Concat(_coreR.Video.All.Where(where))
      .Concat(_coreR.VideoClip.All.Where(where))
      .Concat(_coreR.VideoImage.All.Where(where));

  public IEnumerable<MediaItemM> GetModified() =>
    _coreR.Image.All.Where(x => x.IsOnlyInDb).Cast<MediaItemM>()
      .Concat(_coreR.Video.All.Where(x => x.IsOnlyInDb));

  private void _modify(IEnumerable<MediaItemM> items) =>
    _changeMetadata(items.ToArray(), null);

  public void ModifyIfContains(GeoLocationM gl) =>
    _modify(GetAll(x => ReferenceEquals(x.GeoLocation, gl)));

  public void ModifyIfContains(PersonM person) =>
    _modify(GetAll(mi => mi.GetPeople().Contains(person)));

  public void ModifyIfContains(KeywordM keyword) =>
    _modify(GetAll(mi => mi.GetKeywords().Contains(keyword)));

  public void ModifyIfContains(SegmentM[] segments) =>
    _modify(segments.GetMediaItems());

  public void RemovePerson(PersonM person) =>
    TogglePerson(GetAll(mi => mi.People?.Contains(person) == true).ToArray(), person);

  public void TogglePerson(SegmentM[] segments) =>
    _changeMetadata(segments.GetMediaItems().ToArray(), mi => {
      if (mi.People == null) return;
      foreach (var p in mi.Segments!.GetPeople().Intersect(mi.People).ToArray())
        mi.People = mi.People.Toggle(p, true);
    });

  public void TogglePerson(MediaItemM[] items, PersonM person) =>
    _changeMetadata(items, mi => mi.People = mi.People.Toggle(person, true));

  public void RemoveKeyword(KeywordM keyword) =>
    ToggleKeyword(GetAll(mi => mi.Keywords?.Contains(keyword) == true).ToArray(), keyword);

  public void ToggleKeyword(MediaItemM[] items, KeywordM keyword) =>
    keyword.Toggle(items, Modify, () => RaiseMetadataChanged(items));

  private void _changeMetadata(MediaItemM[] items, Action<MediaItemM>? action) {
    if (items.Length == 0) return;
    foreach (var mi in items) {
      action?.Invoke(mi);
      Modify(mi);
    }

    RaiseMetadataChanged(items);
  }

  public void AddSegment(SegmentM segment) {
    segment.MediaItem.Segments ??= [];
    segment.MediaItem.Segments.Add(segment);
    Modify(segment.MediaItem);
    RaiseMetadataChanged([segment.MediaItem]);
  }

  public void RemoveSegments(IList<SegmentM> segments) {
    foreach (var miSeg in segments.GroupBy(x => x.MediaItem).Where(x => x.Key.Segments != null)) {
      miSeg.Key.Segments = miSeg.Key.Segments!.Except(miSeg).ToList().NullIfEmpty();
      Modify(miSeg.Key);
    }
    
    RaiseMetadataChanged(segments.GetMediaItems().ToArray());
  }

  public void SetGeoName(MediaItemM[] items, GeoNameM geoName) =>
    _changeMetadata(items, mi =>
      _coreR.MediaItemGeoLocation.ItemUpdate(new(mi,
        _coreR.GeoLocation.GetOrCreate(null, null, null, geoName).Result)));

  public void SetRating(MediaItemM[] items, RatingM rating) =>
    _changeMetadata(items, mi => mi.Rating = rating.Value);

  public IEnumerable<MediaItemM> GetItems(object item, bool recursive) =>
    item switch {
      RatingTreeM rating => GetItems(rating.Rating),
      PersonM person => GetItems(person),
      PersonM[] people => GetItems(people),
      SegmentM[] segments => GetItems(segments),
      KeywordM keyword => GetItems(keyword, recursive),
      GeoNameM geoName => GetItems(geoName, recursive),
      _ => Array.Empty<MediaItemM>()
    };

  public IEnumerable<MediaItemM> GetItems(KeywordM keyword, bool recursive) {
    var arr = recursive ? keyword.Flatten().ToArray() : [keyword];
    return GetAll(x => x.Keywords?.Any(k => arr.Any(ar => ReferenceEquals(ar, k))) == true);
  }

  public IEnumerable<MediaItemM> GetItems(GeoNameM geoName, bool recursive) {
    var arr = recursive ? geoName.Flatten().ToArray() : [geoName];
    return GetAll(x => arr.Any(ar => ReferenceEquals(ar, x.GeoLocation?.GeoName))).OrderBy(mi => mi.FileName);
  }

  public IEnumerable<MediaItemM> GetItems(RatingM rating) =>
    GetAll(mi => mi.Rating == rating.Value);

  public IEnumerable<MediaItemM> GetItems(PersonM person) =>
    GetAll(mi => mi.People?.Contains(person) == true ||
                 mi.Segments?.Any(s => s.Person == person) == true)
      .OrderBy(mi => mi.FileName);

  public IEnumerable<MediaItemM> GetItems(PersonM[] people) =>
    GetAll(mi => mi.GetPeople().Intersect(people).Any()).OrderBy(mi => mi.FileName);

  public IEnumerable<MediaItemM> GetItems(SegmentM[] segments) =>
    segments.GetMediaItems();

  public void Rotate(RealMediaItemM[] items, Orientation rotation) {
    foreach (var mi in items) {
      mi.Orientation = rotation.SwapRotateIf(mi is not ImageM).Rotate(mi.Orientation);
      Modify(mi);
    }

    RaiseOrientationChanged(items);
  }
}