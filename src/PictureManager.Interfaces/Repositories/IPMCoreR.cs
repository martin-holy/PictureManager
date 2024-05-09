using PictureManager.Interfaces.Models;

namespace PictureManager.Interfaces.Repositories;

public interface IPMCoreR {
  public IFolderR Folder { get; }
  public IKeywordR Keyword { get; }
  public IRepository<IMediaItemM> MediaItem { get; }
  public IRepository<IPersonM> Person { get; }
  public IRepository<ISegmentM> Segment { get; }
}