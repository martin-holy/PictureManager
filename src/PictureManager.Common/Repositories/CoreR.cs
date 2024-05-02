using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using PictureManager.Interfaces.Models;
using PictureManager.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Common.Repositories;

public sealed class CoreR : SimpleDB, ICoreR {
  public delegate Dictionary<string, string> FileOperationDeleteFunc(List<string> items, bool recycle, bool silent);
  public static FileOperationDeleteFunc FileOperationDelete { get; set; }
  public bool IsCopyMoveInProgress { get; set; }

  public CategoryGroupR CategoryGroup { get; }
  public FavoriteFolderR FavoriteFolder { get; }
  public FolderKeywordR FolderKeyword { get; }
  public FolderR Folder { get; }
  public GeoNameR GeoName { get; }
  public GeoLocationR GeoLocation { get; }
  public ImageR Image { get; }
  public KeywordR Keyword { get; }
  public MediaItemR MediaItem { get; }
  public PersonR Person { get; }
  public SegmentR Segment { get; }
  public VideoClipR VideoClip { get; }
  public VideoImageR VideoImage { get; }
  public VideoR Video { get; }
  public ViewerR Viewer { get; }

  public MediaItemGeoLocationR MediaItemGeoLocation { get; }
  public VideoItemsOrderR VideoItemsOrder { get; }

  IFolderR ICoreR.Folder => Folder;
  IKeywordR ICoreR.Keyword => Keyword;
  IRepository<IMediaItemM> ICoreR.MediaItem => MediaItem;
  IRepository<IPersonM> ICoreR.Person => Person;
  IRepository<ISegmentM> ICoreR.Segment => Segment;

  public CoreR() : base("db") {
    CategoryGroup = new(this);
    FavoriteFolder = new(this);
    FolderKeyword = new(this);
    Folder = new(this);
    GeoName = new(this);
    GeoLocation = new(this);
    Image = new(this);
    Keyword = new(this);
    MediaItem = new(this);
    Person = new(this);
    Segment = new(this);
    VideoClip = new(this);
    VideoImage = new(this);
    Video = new(this);
    Viewer = new(this);

    MediaItemGeoLocation = new(this);
    VideoItemsOrder = new(this);
  }

  public void AddDataAdapters() {
    AddTableDataAdapter(CategoryGroup);
    AddTableDataAdapter(Keyword);
    AddTableDataAdapter(Folder); // needs to be before Viewers, FavoriteFolders and FolderKeywords
    AddTableDataAdapter(FolderKeyword); // needs to be before Viewers
    AddTableDataAdapter(Viewer);
    AddTableDataAdapter(Person); // needs to be before Segments
    AddTableDataAdapter(GeoName);
    AddTableDataAdapter(GeoLocation);
    AddTableDataAdapter(Image);
    AddTableDataAdapter(Video);
    AddTableDataAdapter(VideoClip);
    AddTableDataAdapter(VideoImage);
    AddTableDataAdapter(FavoriteFolder);
    AddTableDataAdapter(Segment);

    AddRelationDataAdapter(MediaItemGeoLocation);
    AddRelationDataAdapter(VideoItemsOrder);
  }

  public static Dictionary<string, IEnumerable<T>> GetAsDriveRelated<T>(IEnumerable<T> source, Func<T, ITreeItem> folder) =>
    source
      .GroupBy(x => Tree.GetParentOf<DriveM>(folder(x)))
      .ToDictionary(x => x.Key.Name, x => x.AsEnumerable());

  public void AttachEvents() {
    CategoryGroup.ItemDeletedEvent += OnCategoryGroupDeleted;

    Folder.ItemCreatedEvent += OnFolderCreated;
    Folder.ItemRenamedEvent += OnFolderRenamed;
    Folder.ItemDeletedEvent += OnFolderDeleted;
    Folder.ItemsDeletedEvent += OnFoldersDeleted;

    GeoLocation.ItemDeletedEvent += OnGeoLocationDeleted;
    GeoLocation.ItemUpdatedEvent += OnGeoLocationUpdated;

    GeoName.ItemDeletedEvent += OnGeoNameDeleted;

    Keyword.ItemDeletedEvent += OnKeywordDeleted;
    Keyword.ItemRenamedEvent += OnKeywordRenamed;

    MediaItem.ItemDeletedEvent += OnMediaItemDeleted;
    MediaItem.OrientationChangedEvent += OnMediaItemsOrientationChanged;

    Person.ItemDeletedEvent += OnPersonDeleted;
    Person.ItemRenamedEvent += OnPersonRenamed;

    Segment.ItemCreatedEvent += OnSegmentCreated;
    Segment.ItemDeletedEvent += OnSegmentDeleted;
    Segment.ItemsDeletedEvent += OnSegmentsDeleted;
    Segment.SegmentsKeywordsChangedEvent += OnSegmentsKeywordsChanged;
    Segment.SegmentPersonChangedEvent += OnSegmentPersonChanged;
    Segment.SegmentsPersonChangedEvent += OnSegmentsPersonChanged;
  }

  private void OnCategoryGroupDeleted(object sender, CategoryGroupM item) {
    Keyword.MoveGroupItemsToRoot(item);
    Person.MoveGroupItemsToRoot(item);
  }

  private void OnFolderCreated(object sender, FolderM item) {
    FolderKeyword.LoadIfContains((FolderM)item.Parent);
  }

  private void OnFolderRenamed(object sender, FolderM item) {
    FolderKeyword.LoadIfContains(item);
  }

  private void OnFoldersDeleted(object sender, IList<FolderM> item) {
    FolderKeyword.Reload();
  }

  private void OnFolderDeleted(object sender, FolderM item) {
    FavoriteFolder.ItemDeleteByFolder(item);
    MediaItem.ItemsDelete(item.MediaItems.Cast<MediaItemM>().ToArray());
  }

  private void OnGeoLocationUpdated(object sender, GeoLocationM item) {
    MediaItem.ModifyIfContains(item);
  }

  private void OnGeoLocationDeleted(object sender, GeoLocationM item) {
    MediaItem.ModifyIfContains(item);
  }

  private void OnGeoNameDeleted(object sender, GeoNameM item) {
    GeoLocation.RemoveGeoName(item);
  }

  private void OnKeywordDeleted(object sender, KeywordM item) {
    Person.RemoveKeyword(item);
    Segment.RemoveKeyword(item);
    MediaItem.RemoveKeyword(item);
  }

  private void OnKeywordRenamed(object sender, KeywordM item) {
    MediaItem.ModifyIfContains(item);
  }

  private void OnMediaItemDeleted(object sender, MediaItemM item) {
    Segment.ItemsDelete(item.Segments?.ToArray());
    if (item.GeoLocation != null)
      MediaItemGeoLocation.IsModified = true;
  }

  private void OnMediaItemsOrientationChanged(object sender, RealMediaItemM[] items) {
    foreach (var rmi in items) {
      rmi.SetThumbSize(true);
      File.Delete(rmi.FilePathCache);
    }
  }

  private void OnPersonDeleted(object sender, PersonM item) {
    MediaItem.RemovePerson(item);
    Segment.RemovePerson(item);
  }

  private void OnPersonRenamed(object sender, PersonM item) {
    MediaItem.ModifyIfContains(item);
  }

  private void OnSegmentCreated(object sender, SegmentM e) {
    MediaItem.AddSegment(e);
  }

  private void OnSegmentDeleted(object sender, SegmentM item) {
    Person.OnSegmentPersonChanged(item, item.Person, null);
  }

  private void OnSegmentsDeleted(object sender, IList<SegmentM> items) {
    MediaItem.RemoveSegments(items);
  }

  private void OnSegmentsKeywordsChanged(object sender, SegmentM[] items) {
    MediaItem.ModifyIfContains(items);
  }

  private void OnSegmentPersonChanged(object sender, (SegmentM, PersonM, PersonM) e) {
    Person.OnSegmentPersonChanged(e.Item1, e.Item2, e.Item3);
  }

  private void OnSegmentsPersonChanged(object sender, (SegmentM[], PersonM, PersonM[]) e) {
    Person.OnSegmentsPersonChanged(e.Item1, e.Item2, e.Item3);
    MediaItem.TogglePerson(e.Item1);
  }
}