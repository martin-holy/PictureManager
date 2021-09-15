namespace PictureManager.Domain {
  public enum BackgroundBrush { Default = 0, Marked = 1, OrThis = 2, AndThis = 3, Hidden = 4 }
  public enum Category { FavoriteFolders = 0, Folders = 1, Ratings = 2, People = 3, FolderKeywords = 4, Keywords = 5, Filters = 6, Viewers = 7, GeoNames = 8, SqlQueries = 9, MediaItemSizes = 10, MediaItemClips = 11 }
  public enum MediaType { Image = 0, Video = 1 }
  public enum MediaOrientation { Normal = 1, FlipHorizontal = 2, Rotate180 = 3, FlipVertical = 4, Transpose = 5, Rotate270 = 6, Transverse = 7, Rotate90 = 8 }
  public enum CollisionResult { Rename, Replace, Skip }
  public enum FileOperationMode { Copy, Move, Delete }
  public enum IconName { Folder, FolderStar, FolderLock, FolderOpen, FolderPuzzle, Star, People, PeopleMultiple, Tag, TagLabel, Filter, Eye, DatabaseSql, Bug, LocationCheckin, Notification, Cd, Drive, DriveError, Edit, Cancel, Save, Ruler, Settings, Question, Information, RotateClockwise, RotateLeft, RotateRight, Pin, Calendar, Magnify, Image, ImageMultiple, PageUpload, MovieClapper, SoundMute, Sound3, Clock, RunFast, Checkmark, XCross, Sort, Compare, Refresh, Group, Equals }
}
