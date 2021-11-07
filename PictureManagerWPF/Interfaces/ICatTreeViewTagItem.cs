using MH.UI.WPF.Interfaces;

namespace PictureManager.Interfaces {
  public interface ICatTreeViewTagItem : ICatTreeViewItem {
    int PicCount { get; set; }
  }
}
