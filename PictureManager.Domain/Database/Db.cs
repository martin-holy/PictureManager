using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Database;

public sealed class Db : SimpleDB {
  public CategoryGroupsDataAdapter CategoryGroups { get; }
  public FavoriteFoldersDataAdapter FavoriteFolders { get; }
  public FolderKeywordsDataAdapter FolderKeywords { get; }
  public FoldersDataAdapter Folders { get; }
  public GeoNamesDataAdapter GeoNames { get; }
  public ImagesDA Images { get; }
  public KeywordsDataAdapter Keywords { get; }
  public MediaItemsDataAdapter MediaItems { get; }
  public PeopleDataAdapter People { get; }
  public SegmentsDataAdapter Segments { get; }
  public VideoClipsDataAdapter VideoClips { get; }
  public VideoImagesDA VideoImages { get; }
  public VideosDA Videos { get; }
  public ViewersDataAdapter Viewers { get; }

  public VideoItemsOrderDA VideoItemsOrder { get; }
  public Db() {
    CategoryGroups = new(this);
    FavoriteFolders = new(this);
    FolderKeywords = new(this);
    Folders = new(this);
    GeoNames = new();
    Images = new(this);
    Keywords = new(this);
    MediaItems = new(this);
    People = new(this);
    Segments = new(this);
    VideoClips = new(this);
    VideoImages = new(this);
    Videos = new(this);
    Viewers = new(this);
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
    AddDataAdapter(MediaItems);
    AddTableDataAdapter(Images);
    AddTableDataAdapter(Videos);
    AddTableDataAdapter(VideoClips);
    AddTableDataAdapter(VideoImages);
    AddTableDataAdapter(FavoriteFolders);
    AddTableDataAdapter(Segments);
    AddRelationDataAdapter(VideoItemsOrder);
  }

  public static Dictionary<string, IEnumerable<T>> GetAsDriveRelated<T>(IEnumerable<T> source, Func<T, ITreeItem> folder) =>
    source
      .GroupBy(x => Tree.GetParentOf<DriveM>(folder(x)))
      .ToDictionary(x => x.Key.Name, x => x.AsEnumerable());
}