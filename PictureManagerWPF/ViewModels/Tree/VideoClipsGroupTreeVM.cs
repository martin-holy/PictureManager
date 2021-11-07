using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class VideoClipsGroupTreeVM : CatTreeViewItem, ICatTreeViewGroup {
    public VideoClipsGroupM Model { get; }

    public VideoClipsGroupTreeVM(VideoClipsGroupM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
