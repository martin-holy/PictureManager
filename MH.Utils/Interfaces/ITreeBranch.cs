using System.Collections.ObjectModel;

namespace MH.Utils.Interfaces {
  public interface ITreeBranch : ITreeLeaf {
    ObservableCollection<ITreeLeaf> Items { get; set; }
  }
}
