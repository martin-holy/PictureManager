using MH.Utils.Interfaces;

namespace PictureManager.Domain.Models; 

public class DriveM : FolderM {
  public string SerialNumber { get; set; }

  public DriveM(int id, string name, ITreeItem parent, string sn) : base(id, name, parent) {
    SerialNumber = sn;
  }
}