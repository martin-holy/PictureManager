using MH.UI.WPF.BaseClasses;
using PictureManager.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public class CatTreeViewTagItemBase : CatTreeViewItem, ICatTreeViewTagItem {
    private int _picCount;
    public int PicCount { get => _picCount; set { _picCount = value; OnPropertyChanged(); } }
  }
}
