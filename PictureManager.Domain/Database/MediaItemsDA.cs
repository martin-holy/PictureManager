using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Database;

public sealed class MediaItemsDA : TableDataAdapter<MediaItemM> {
  private readonly Db _db;
  private static readonly string[] _supportedImageExts = { ".jpg", ".jpeg" };
  private static readonly string[] _supportedVideoExts = { ".mp4" };

  public MediaItemsM Model { get; }

  public event EventHandler<ObjectEventArgs<MediaItemM>> ItemRenamedEvent = delegate { };
  public event DataEventHandler<MediaItemM[]> MetadataChangedEvent = delegate { };
  public event DataEventHandler<RealMediaItemM[]> OrientationChangedEvent = delegate { };

  public MediaItemsDA(Db db) : base(string.Empty, 0) {
    _db = db;
    _db.ReadyEvent += delegate { OnDbReady(); };
    Model = new(this);
  }

  private void RaiseItemRenamed(MediaItemM item) => ItemRenamedEvent(this, new(item));
  public void RaiseMetadataChanged(MediaItemM[] items) => MetadataChangedEvent(items);
  public void RaiseOrientationChanged(RealMediaItemM[] items) => OrientationChangedEvent(items);

  private void OnDbReady() {
    MaxId = _db.Images.MaxId;

    _db.Images.ItemCreatedEvent += (_, e) => OnItemCreated(e.Data);
    _db.Images.ItemDeletedEvent += (_, e) => OnItemDeleted(e.Data);
    _db.Images.ItemsDeletedEvent += (_, e) => OnItemsDeleted(e.Data.Cast<MediaItemM>().ToArray());
    _db.Videos.ItemCreatedEvent += (_, e) => OnItemCreated(e.Data);
    _db.Videos.ItemDeletedEvent += (_, e) => OnItemDeleted(e.Data);
    _db.Videos.ItemsDeletedEvent += (_, e) => OnItemsDeleted(e.Data.Cast<MediaItemM>().ToArray());
    _db.VideoClips.ItemCreatedEvent += (_, e) => OnItemCreated(e.Data);
    _db.VideoClips.ItemDeletedEvent += (_, e) => OnItemDeleted(e.Data);
    _db.VideoClips.ItemsDeletedEvent += (_, e) => OnItemsDeleted(e.Data.Cast<MediaItemM>().ToArray());
    _db.VideoImages.ItemCreatedEvent += (_, e) => OnItemCreated(e.Data);
    _db.VideoImages.ItemDeletedEvent += (_, e) => OnItemDeleted(e.Data);
    _db.VideoImages.ItemsDeletedEvent += (_, e) => OnItemsDeleted(e.Data.Cast<MediaItemM>().ToArray());
  }

  protected override void OnItemCreated(MediaItemM item) {
    if (item is RealMediaItemM rmi)
      rmi.Folder.MediaItems.Add(rmi);

    Model.UpdateItemsCount();
    RaiseItemCreated(item);
  }

  protected override void OnItemDeleted(MediaItemM item) {
    Model.UpdateItemsCount();
    Model.UpdateModifiedCount();
    RaiseItemDeleted(item);
  }

  public void OnItemDeletedCommon(MediaItemM item) {
    File.Delete(item.FilePathCache);
    item.People = null;
    item.Keywords = null;
    item.Segments = null;

    if (item is RealMediaItemM rmi) {
      rmi.Folder.MediaItems.Remove(rmi);
      rmi.Folder = null;
    }
  }

  protected override void OnItemsDeleted(IList<MediaItemM> items) =>
    RaiseItemsDeleted(items);

  public override int GetNextId() {
    var id = ++MaxId;
    _db.Images.MaxId = id;
    _db.Videos.MaxId = id;
    _db.VideoClips.MaxId = id;
    _db.VideoImages.MaxId = id;
    return id;
  }

  public override MediaItemM GetById(string id, bool nullable = false) {
    var intId = int.Parse(id);
    if (_db.Images.AllDict.TryGetValue(intId, out var img)) return img;
    if (_db.Videos.AllDict.TryGetValue(intId, out var vid)) return vid;
    if (_db.VideoClips.AllDict.TryGetValue(intId, out var vc)) return vc;
    if (_db.VideoImages.AllDict.TryGetValue(intId, out var vi)) return vi;
    return null;
  }

  public RealMediaItemM ItemCreate(FolderM folder, string fileName) {
    if (_supportedImageExts.Any(x => fileName.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
      return _db.Images.ItemCreate(folder, fileName);

    if (_supportedVideoExts.Any(x => fileName.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
      return _db.Videos.ItemCreate(folder, fileName);

    throw new($"Can not create item. Unknown MediaItem type. {fileName}");
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
    Model.OnPropertyChanged(nameof(Model.Current));
    RaiseItemRenamed(item);
  }

  public void ItemMove(RealMediaItemM item, FolderM folder, string fileName) {
    item.FileName = fileName;
    item.Folder.MediaItems.Remove(item);
    item.Folder = folder;
    item.Folder.MediaItems.Add(item);
    ModifyOnlyDA(item);
  }

  public RealMediaItemM ItemCopy(RealMediaItemM item, FolderM folder, string fileName) =>
    item switch {
      ImageM img => _db.Images.ItemCopy(img, folder, fileName),
      VideoM vid => _db.Videos.ItemCopy(vid, folder, fileName),
      _ => null
    };

  public void ItemCopyCommon(RealMediaItemM item, RealMediaItemM copy) {
    copy.Width = item.Width;
    copy.Height = item.Height;
    copy.Orientation = item.Orientation;
    copy.Rating = item.Rating;
    copy.Comment = item.Comment;
    copy.IsOnlyInDb = item.IsOnlyInDb;

    if (item.GeoLocation != null) {
      copy.GeoLocation = item.GeoLocation;
      _db.MediaItemGeoLocation.IsModified = true;
    }

    if (item.People != null)
      copy.People = new(item.People);

    if (item.Keywords != null)
      copy.Keywords = new(item.Keywords);

    if (item.Segments != null)
      foreach (var segment in item.Segments)
        _db.Segments.ItemCopy(segment, copy);
  }

  private void ModifyOnlyDA(RealMediaItemM mi) {
    switch (mi) {
      case ImageM img: _db.Images.Modify(img); break;
      case VideoM vid: _db.Videos.Modify(vid); break;
    }
  }

  public override void Modify(MediaItemM mi) {
    switch (mi) {
      case ImageM img:
        _db.Images.Modify(img);
        img.IsOnlyInDb = true;
        break;
      case VideoM vid:
        _db.Videos.Modify(vid);
        vid.IsOnlyInDb = true;
        break;
      case VideoClipM vc: _db.VideoClips.Modify(vc); break;
      case VideoImageM vi: _db.VideoImages.Modify(vi); break;
    }
  }

  public override void ItemDelete(MediaItemM mi, bool singleDelete = true) {
    switch (mi) {
      case ImageM img: _db.Images.ItemDelete(img); break;
      case VideoM vid: _db.Videos.ItemDelete(vid); break;
      case VideoClipM vc: _db.VideoClips.ItemDelete(vc); break;
      case VideoImageM vi: _db.VideoImages.ItemDelete(vi); break;
    }
  }

  public override void ItemsDelete(IList<MediaItemM> items) {
    _db.Images.ItemsDelete(items.OfType<ImageM>().ToArray());
    _db.Videos.ItemsDelete(items.OfType<VideoM>().ToArray());
    _db.VideoClips.ItemsDelete(items.OfType<VideoClipM>().ToArray());
    _db.VideoImages.ItemsDelete(items.OfType<VideoImageM>().ToArray());
  }

  public override IEnumerable<MediaItemM> GetAll(Func<MediaItemM, bool> where) =>
    _db.Images.All.Where(where)
      .Concat(_db.Videos.All.Where(where))
      .Concat(_db.VideoClips.All.Where(where))
      .Concat(_db.VideoImages.All.Where(where));

  public IEnumerable<MediaItemM> GetModified() =>
    _db.Images.All.Where(x => x.IsOnlyInDb).Cast<MediaItemM>()
      .Concat(_db.Videos.All.Where(x => x.IsOnlyInDb));

  private void Modify(IEnumerable<MediaItemM> items) =>
    ChangeMetadata(items.ToArray(), null);

  public void ModifyIfContains(GeoLocationM gl) =>
    Modify(GetAll(x => ReferenceEquals(x.GeoLocation, gl)));

  public void ModifyIfContains(PersonM person) =>
    Modify(GetAll(mi => mi.GetPeople().Contains(person)));

  public void ModifyIfContains(KeywordM keyword) =>
    Modify(GetAll(mi => mi.GetKeywords().Contains(keyword)));

  public void ModifyIfContains(SegmentM[] segments) =>
    Modify(segments.GetMediaItems());

  public void RemovePerson(PersonM person) =>
    TogglePerson(GetAll(mi => mi.People?.Contains(person) == true).ToArray(), person);

  public void TogglePerson(SegmentM[] segments) =>
    ChangeMetadata(segments.GetMediaItems().ToArray(), mi => {
      if (mi.People == null) return;
      foreach (var p in mi.Segments.GetPeople().Intersect(mi.People).ToArray())
        mi.People = mi.People.Toggle(p, true);
    });

  public void TogglePerson(MediaItemM[] items, PersonM person) =>
    ChangeMetadata(items, mi => mi.People = mi.People.Toggle(person, true));

  public void RemoveKeyword(KeywordM keyword) =>
    ToggleKeyword(GetAll(mi => mi.Keywords?.Contains(keyword) == true).ToArray(), keyword);

  public void ToggleKeyword(MediaItemM[] items, KeywordM keyword) =>
    keyword.Toggle(items, Modify, () => RaiseMetadataChanged(items));

  private void ChangeMetadata(MediaItemM[] items, Action<MediaItemM> action) {
    if (items.Length == 0) return;
    foreach (var mi in items) {
      action?.Invoke(mi);
      Modify(mi);
    }

    RaiseMetadataChanged(items);
  }

  public void AddSegment(SegmentM segment) {
    segment.MediaItem.Segments ??= new();
    segment.MediaItem.Segments.Add(segment);
    Modify(segment.MediaItem);
    RaiseMetadataChanged(new[] { segment.MediaItem });
  }

  public void RemoveSegments(IList<SegmentM> segments) {
    foreach (var segment in segments) {
      segment.MediaItem.Segments = segment.MediaItem.Segments.Toggle(segment, true);
      Modify(segment.MediaItem);
    }
    
    RaiseMetadataChanged(segments.GetMediaItems().ToArray());
  }

  public void SetGeoName(MediaItemM[] items, GeoNameM geoName) =>
    ChangeMetadata(items, mi =>
      _db.MediaItemGeoLocation.ItemUpdate(new(mi,
        _db.GeoLocations.GetOrCreate(null, null, null, geoName).Result)));

  public void SetRating(MediaItemM[] items, RatingM rating) =>
    ChangeMetadata(items, mi => mi.Rating = rating.Value);

  public IEnumerable<MediaItemM> GetItems(object item, bool recursive) =>
    item switch {
      RatingTreeM rating => GetItems(rating.Rating),
      PersonM person => GetItems(person),
      KeywordM keyword => GetItems(keyword, recursive),
      GeoNameM geoName => GetItems(geoName, recursive),
      _ => Array.Empty<MediaItemM>()
    };

  public IEnumerable<MediaItemM> GetItems(KeywordM keyword, bool recursive) {
    var set = (recursive ? keyword.Flatten() : new[] { keyword }).ToHashSet();

    return GetAll(mi =>
      mi.Keywords?.Any(k => set.Contains(k)) == true ||
      mi.Segments?.Any(s => s.Keywords?.Any(k => set.Contains(k)) == true) == true);
  }

  public IEnumerable<MediaItemM> GetItems(GeoNameM geoName, bool recursive) {
    var set = (recursive ? geoName.Flatten() : new[] { geoName }).ToHashSet();
    return GetAll(x => set.Contains(x.GeoLocation?.GeoName)).OrderBy(mi => mi.FileName);
  }

  public IEnumerable<MediaItemM> GetItems(RatingM rating) =>
    GetAll(mi => mi.Rating == rating.Value);

  public IEnumerable<MediaItemM> GetItems(PersonM person) =>
    GetAll(mi => mi.People?.Contains(person) == true ||
                 mi.Segments?.Any(s => s.Person == person) == true)
      .OrderBy(mi => mi.FileName);

  public void Rotate(RealMediaItemM[] items, Orientation rotation) {
    foreach (var mi in items) {
      mi.Orientation = rotation.SwapRotateIf(mi is not ImageM).Rotate(mi.Orientation);
      Modify(mi);
    }

    RaiseOrientationChanged(items);
  }
}