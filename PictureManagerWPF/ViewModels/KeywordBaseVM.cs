using System.Collections.ObjectModel;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public class KeywordBaseVM : ITreeBranch {
    #region ITreeBranch implementation
    public object Parent { get; set; }
    public ObservableCollection<object> Items { get; set; } = new();
    #endregion

    public KeywordM Model { get; }

    public KeywordBaseVM(KeywordM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
