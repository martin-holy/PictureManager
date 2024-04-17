using PictureManager.Interfaces.Models;

namespace PictureManager.Interfaces.Repositories;

public interface IFolderR {
  public IFolderM GetFolder(string folderPath);
}