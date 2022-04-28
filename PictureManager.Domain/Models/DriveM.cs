using MH.Utils.Interfaces;

namespace PictureManager.Domain.Models {
  public class DriveM : FolderM {
    public DriveM(int id, string name, ITreeItem parent) : base(id, name, parent) { }
  }
}
