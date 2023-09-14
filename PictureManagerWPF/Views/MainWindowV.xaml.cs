using PictureManager.ViewModels;

namespace PictureManager.Views;

public partial class MainWindowV {
  public MainWindowV() {
    InitializeComponent();
    HideMouseTimer.Init(this, App.Core.MediaViewerM.PresentationPanel);
  }
}