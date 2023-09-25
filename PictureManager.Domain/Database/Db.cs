using MH.Utils;

namespace PictureManager.Domain.Database;

public sealed class Db : SimpleDB {
  public CategoryGroupsDataAdapter CategoryGroups { get; set; }
  public FavoriteFoldersDataAdapter FavoriteFolders { get; set; }
  public FolderKeywordsDataAdapter FolderKeywords { get; set; }
  public FoldersDataAdapter Folders { get; set; }
  public GeoNamesDataAdapter GeoNames { get; set; }
  public KeywordsDataAdapter Keywords { get; set; }
  public MediaItemsDataAdapter MediaItems { get; set; }
  public PeopleDataAdapter People { get; set; }
  public SegmentsDataAdapter Segments { get; set; }
  public VideoClipsDataAdapter VideoClips { get; set; }
  public VideoClipsGroupsDataAdapter VideoClipsGroups { get; set; }
  public ViewersDataAdapter Viewers { get; set; }

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
}