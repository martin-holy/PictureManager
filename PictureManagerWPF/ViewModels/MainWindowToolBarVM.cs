using PictureManager.Domain;

namespace PictureManager.ViewModels {
  public sealed class MainWindowToolBarVM {
    public Core CoreM { get; }
    public AppCore CoreVM { get; }

    public MainWindowToolBarVM(Core coreM, AppCore coreVM) {
      CoreM = coreM;
      CoreVM = coreVM;
    }
  }
}
