using PictureManager.Domain;
using PictureManager.ViewModels;

namespace PictureManager.Views;

public partial class MainWindowV {
  public MainWindowV() {
    InitializeComponent();
    HideMouseTimer.Init(this, Core.MediaViewerM.PresentationPanel);
  }
}