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
  public KeywordsDataAdapter Keywords { get; }
  public MediaItemsDataAdapter MediaItems { get; }
  public PeopleDataAdapter People { get; }
  public SegmentsDataAdapter Segments { get; }
  public VideoClipsDataAdapter VideoClips { get; }
  public VideoClipsGroupsDataAdapter VideoClipsGroups { get; }
  public ViewersDataAdapter Viewers { get; }

  public Db() {
    CategoryGroups = new(this);
    FavoriteFolders = new(this);
    FolderKeywords = new(this);
    Folders = new(this);
    GeoNames = new();
    Keywords = new(this);
    MediaItems = new(this);
    People = new(this);
    Segments = new(this);
    VideoClips = new(this);
    VideoClipsGroups = new(this, VideoClips.Model);
    Viewers = new(this);
  }

  public void AddDataAdapters() {
    AddDataAdapter(CategoryGroups);
    AddDataAdapter(Keywords);
    AddDataAdapter(Folders); // needs to be before Viewers, FavoriteFolders and FolderKeywords
    AddDataAdapter(FolderKeywords); // needs to be before Viewers
    AddDataAdapter(Viewers);
    AddDataAdapter(People); // needs to be before Segments
    AddDataAdapter(GeoNames);
    AddDataAdapter(MediaItems);
    AddDataAdapter(VideoClipsGroups); // needs to be before VideoClips
    AddDataAdapter(VideoClips);
    AddDataAdapter(FavoriteFolders);
    AddDataAdapter(Segments);
  }

  public static Dictionary<string, IEnumerable<T>> GetAsDriveRelated<T>(IEnumerable<T> source, Func<T, ITreeItem> folder) =>
    source
      .GroupBy(x => Tree.GetParentOf<DriveM>(folder(x)))
      .ToDictionary(x => x.Key.Name, x => x.AsEnumerable());
}