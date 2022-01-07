namespace PictureManager.Domain {
  public enum Category { FavoriteFolders = 0, Folders = 1, Ratings = 2, People = 3, FolderKeywords = 4, Keywords = 5, Filters = 6, Viewers = 7, GeoNames = 8, SqlQueries = 9, MediaItemSizes = 10, VideoClips = 11 }
  public enum MediaType { Image = 0, Video = 1 }
  public enum MediaOrientation { Normal = 1, FlipHorizontal = 2, Rotate180 = 3, FlipVertical = 4, Transpose = 5, Rotate270 = 6, Transverse = 7, Rotate90 = 8 }
  public enum CollisionResult { Rename, Replace, Skip }
  public enum FileOperationMode { Copy, Move, Delete }
  public enum DisplayFilter { None = 0, And = 1, Or = 2, Not = 3 }
}
