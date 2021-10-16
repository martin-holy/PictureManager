using System.Collections.ObjectModel;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class CategoryGroupBaseVM : ITreeBranch {
    #region ITreeBranch implementation
    public object Parent { get; set; }
    public ObservableCollection<object> Items { get; set; } = new();
    #endregion

    public CategoryGroupM Model { get; }

    public CategoryGroupBaseVM(CategoryGroupM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
