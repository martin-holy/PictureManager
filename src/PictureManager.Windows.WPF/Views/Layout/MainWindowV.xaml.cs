using PictureManager.Common;
using PictureManager.Windows.WPF.ViewModels;

namespace PictureManager.Windows.WPF.Views.Layout;

public partial class MainWindowV {
  public MainWindowV() {
    InitializeComponent();
    HideMouseTimer.Init(this, Core.VM.MediaViewer.Slideshow);
  }
}