namespace PictureManager.Domain.Interfaces {
  public interface ITreeLeaf {
    ITreeBranch Parent { get; set; }
  }
}
