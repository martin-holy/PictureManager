using System;
using System.Collections.Generic;
using System.Linq;
using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.Repositories;

public sealed class CoreR : SimpleDB {
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

  public CoreR() {
    CategoryGroup = new(this);
    FavoriteFolder = new(this);
    FolderKeyword = new(this);
    Folder = new();
    GeoName = new();
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
}