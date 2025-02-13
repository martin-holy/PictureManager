using PictureManager.Common;
using AUI = MH.UI.AvaloniaUI;

namespace PictureManager.AvaloniaUI;

public class CoreUI : ICoreP {
  public CoreUI() {
    AUI.Utils.Init.SetDelegates();
    AUI.Resources.Dictionaries.IconToBrush = Res.IconToBrushDic;
  }

  public void AfterInit() {
    // TODO PORT
  }

  public void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) =>
    throw new System.NotImplementedException();
}