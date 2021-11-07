using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class DriveTreeVM : FolderTreeVM {
    public DriveTreeVM(FolderM model, ITreeBranch parent) : base(model, parent) { }
  }
}
