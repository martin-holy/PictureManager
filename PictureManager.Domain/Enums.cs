namespace PictureManager.Domain;

public enum Category { FavoriteFolders = 0, Folders = 1, Ratings = 2, People = 3, FolderKeywords = 4, Keywords = 5, Filters = 6, Viewers = 7, GeoNames = 8, SqlQueries = 9, MediaItemSizes = 10, VideoClips = 11 }
public enum CollisionResult { Rename, Replace, Skip }
public enum FileOperationMode { Copy, Move, Delete }
public enum DisplayFilter { None = 0, And = 1, Or = 2, Not = 3 }
public enum SegmentEditMode { None, Move, ResizeEdge, ResizeCorner, ResizeLeftEdge, ResizeTopEdge, ResizeRightEdge, ResizeBottomEdge }