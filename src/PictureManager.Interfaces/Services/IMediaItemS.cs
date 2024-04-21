using PictureManager.Interfaces.Models;

namespace PictureManager.Interfaces.Services;

public interface IMediaItemS {
  public IMediaItemM GetMediaItem(IFolderM folder, string fileName);
}