using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Database;

public sealed class Db : SimpleDB {
  public CategoryGroupsDA CategoryGroups { get; }
  public FavoriteFoldersDA FavoriteFolders { get; }
  public FolderKeywordsDA FolderKeywords { get; }
  public FoldersDA Folders { get; }
  public GeoNamesDA GeoNames { get; }
  public GeoLocationsDA GeoLocations { get; }
  public ImagesDA Images { get; }
  public KeywordsDA Keywords { get; }
  public MediaItemsDA MediaItems { get; }
  public PeopleDA People { get; }
  public SegmentsDA Segments { get; }
  public VideoClipsDA VideoClips { get; }
  public VideoImagesDA VideoImages { get; }
  public VideosDA Videos { get; }
  public ViewersDA Viewers { get; }

  public MediaItemGeoLocationDA MediaItemGeoLocation { get; }
  public VideoItemsOrderDA VideoItemsOrder { get; }

  public Db() {
    CategoryGroups = new(this);
    FavoriteFolders = new(this);
    FolderKeywords = new(this);
    Folders = new();
    GeoNames = new();
    GeoLocations = new(this);
    Images = new(this);
    Keywords = new(this);
    MediaItems = new(this);
    People = new(this);
    Segments = new(this);
    VideoClips = new(this);
    VideoImages = new(this);
    Videos = new(this);
    Viewers = new(this);

    MediaItemGeoLocation = new(this);
    VideoItemsOrder = new(this);
  }

  public void AddDataAdapters() {
    AddTableDataAdapter(CategoryGroups);
    AddTableDataAdapter(Keywords);
    AddTableDataAdapter(Folders); // needs to be before Viewers, FavoriteFolders and FolderKeywords
    AddTableDataAdapter(FolderKeywords); // needs to be before Viewers
    AddTableDataAdapter(Viewers);
    AddTableDataAdapter(People); // needs to be before Segments
    AddTableDataAdapter(GeoNames);
    AddTableDataAdapter(GeoLocations);
    AddTableDataAdapter(Images);
    AddTableDataAdapter(Videos);
    AddTableDataAdapter(VideoClips);
    AddTableDataAdapter(VideoImages);
    AddTableDataAdapter(FavoriteFolders);
    AddTableDataAdapter(Segments);

    AddRelationDataAdapter(MediaItemGeoLocation);
    AddRelationDataAdapter(VideoItemsOrder);
  }

  public static Dictionary<string, IEnumerable<T>> GetAsDriveRelated<T>(IEnumerable<T> source, Func<T, ITreeItem> folder) =>
    source
      .GroupBy(x => Tree.GetParentOf<DriveM>(folder(x)))
      .ToDictionary(x => x.Key.Name, x => x.AsEnumerable());
}