namespace PictureManager {
  public enum BackgroundBrush { Default = 0, Marked = 1, OrThis = 2, AndThis = 3, Hidden = 4 }
  public enum AppMode { Browser = 0, Viewer = 1 }
  public enum AppProperty { AppCore, WMain, SubmitChanges, FileOperationResult, EditKeywordsFromFolders, MainTreeViewWidth }
  public enum FileOperationMode { Copy, Move, Delete }
  public enum Category { FavoriteFolders = 0, Folders = 1, Ratings = 2, People = 3, FolderKeywords = 4, Keywords = 5, Filters = 6, Viewers = 7, GeoNames = 8, SqlQueries = 9, MediaItemSizes = 10 }
  public enum MediaType { Image = 0, Video = 1 }
  public enum MediaOrientation { Normal = 1, FlipHorizontal = 2, Rotate180 = 3, FlipVertical = 4, Transpose = 5, Rotate270 = 6, Transverse = 7, Rotate90 = 8 }
  public enum IconName { Folder, FolderStar, FolderLock, FolderOpen, Star, People, PeopleMultiple, Tag, TagLabel, Filter, Eye, DatabaseSql, Bug, LocationCheckin, Notification, Cd, Drive, DriveError, Cancel, Save, Ruler }
}