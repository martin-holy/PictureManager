using PictureManager.Interfaces.Models;

namespace PictureManager.Interfaces.Services;

public interface IMediaItemS {
  public IMediaItemM ImportMediaItem(IFolderM folder, string fileName);
}