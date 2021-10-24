using System.Collections.ObjectModel;

namespace PictureManager.Domain.Interfaces {
  public interface ITreeBranch : ITreeLeaf {
    ObservableCollection<ITreeLeaf> Items { get; set; }
  }
}
