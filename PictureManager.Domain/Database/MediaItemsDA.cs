using MH.Utils;
using MH.Utils.BaseClasses;
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

  public MediaItemsDA(Db db) : base(string.Empty, 0) {
    _db = db;
    _db.ReadyEvent += delegate { OnDbReady(); };
    Model = new(this);
  }

  private void RaiseItemRenamed(MediaItemM item) => ItemRenamedEvent(this, new(item));

  private void OnDbReady() {
    MaxId = _db.Images.MaxId;

    _db.Segments.ItemCreatedEvent += (_, e) => {
      Modify(e.Data.MediaItem);
      Model.UpdateModifiedCount();
    };

    _db.Segments.ItemDeletedEvent += (_, e) => {
      Modify(e.Data.MediaItem);
      Model.UpdateModifiedCount();
    };

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

    _db.MediaItemGeoLocation.ItemDeletedEvent += (_, e) => Modify(e.Data.Key);
    _db.MediaItemGeoLocation.ItemCreatedEvent += (_, e) => Modify(e.Data.Key);
    _db.GeoLocations.ItemUpdatedEvent += (_, e) => {
      foreach (var kv in _db.MediaItemGeoLocation.All.Where(x => ReferenceEquals(x.Value, e.Data)))
        Modify(kv.Key);
    };
  }

  protected override void OnItemCreated(MediaItemM item) {
    Model.UpdateItemsCount();
    RaiseItemCreated(item);
  }

  protected override void OnItemDeleted(MediaItemM item) {
    Model.UpdateItemsCount();
    Model.UpdateModifiedCount();
    RaiseItemDeleted(item);
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
    
    Modify(item);
    Model.OnPropertyChanged(nameof(Model.Current));
    RaiseItemRenamed(item);
  }

  public void ItemMove(RealMediaItemM item, FolderM folder, string fileName) {
    item.FileName = fileName;
    item.Folder.MediaItems.Remove(item);
    item.Folder = folder;
    item.Folder.MediaItems.Add(item);
    Modify(item);
  }

  public RealMediaItemM ItemCopy(RealMediaItemM item, FolderM folder, string fileName) =>
    item switch {
      ImageM img => _db.Images.ItemCopy(img, folder, fileName),
      VideoM vid => _db.Videos.ItemCopy(vid, folder, fileName),
      _ => null
    };

  public void ItemCopyCommon(MediaItemM item, MediaItemM copy) {
    copy.Width = item.Width;
    copy.Height = item.Height;
    copy.Orientation = item.Orientation;
    copy.Rating = item.Rating;
    copy.Comment = item.Comment;

    if (_db.MediaItemGeoLocation.All.TryGetValue(item, out var gl))
      _db.MediaItemGeoLocation.ItemCreate(new(copy, gl));

    if (item.People != null)
      copy.People = new(item.People);

    if (item.Keywords != null)
      copy.Keywords = new(item.Keywords);

    if (item.Segments != null)
      foreach (var segment in item.Segments)
        _db.Segments.ItemCopy(segment, copy);
  }

  public override void Modify(MediaItemM mi) {
    switch (mi) {
      case ImageM img: _db.Images.Modify(img); break;
      case VideoM vid: _db.Videos.Modify(vid); break;
      case VideoClipM vc: _db.VideoClips.Modify(vc); break;
      case VideoImageM vi: _db.VideoImages.Modify(vi); break;
    }
  }

  public override void ItemDelete(MediaItemM mi) {
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
}