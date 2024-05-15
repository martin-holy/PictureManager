using MH.Utils.Interfaces;
using PictureManager.Interfaces.Models;

namespace PictureManager.Interfaces.Repositories;

public interface IPMCoreR {
  public IFolderR Folder { get; }
  public IInterfaceTableDataAdapter<IKeywordM> Keyword { get; }
  public IInterfaceTableDataAdapter<IMediaItemM> MediaItem { get; }
  public IInterfaceTableDataAdapter<IPersonM> Person { get; }
  public IInterfaceTableDataAdapter<ISegmentM> Segment { get; }
}