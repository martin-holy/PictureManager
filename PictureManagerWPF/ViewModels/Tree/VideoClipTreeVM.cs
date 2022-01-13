using MH.UI.WPF.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public class VideoClipTreeVM : CatTreeViewItem {
    public VideoClipM Model { get; }

    private string _title;
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

    public VideoClipTreeVM(VideoClipM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
