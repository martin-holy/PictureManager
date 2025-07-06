using Android.App;
using MH.Utils.Interfaces;
using PictureManager.Android.Views;
using PictureManager.Common;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Android;

public class CoreUI: ICoreP {
  private readonly MainActivity _mainActivity;

  public MainWindowV MainWindow { get; private set; } = null!;

  public CoreUI(MainActivity mainActivity) {
    _mainActivity = mainActivity;
    // TODO PORT
    MH.UI.Android.Utils.Init.SetDelegates();
    MH.UI.Android.Utils.Icons.IconNameToColor = Resources.Res.IconToColorDic;
    MH.UI.Android.Utils.DisplayU.Init(Application.Context!.Resources!.DisplayMetrics!);
    MediaItemS.ReadMetadata = ViewModels.MediaItemVM.ReadMetadata;
  }

  public void AfterInit() {
    Core.Settings.MediaItem.MediaItemThumbScale = 0.5;
    MainWindow = new MainWindowV(_mainActivity).Bind(Core.VM.MainWindow);
    _attachEvents();
  }

  public void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) => throw new System.NotImplementedException();
  public string GetFilePathCache(FolderM folder, string fileNameCache) => throw new System.NotImplementedException();
  public string GetFolderPathCache(FolderM folder) => throw new System.NotImplementedException();

  private void _attachEvents() {
    Core.R.Folder.Tree.ItemSelectedEvent += _onFolderTreeItemSelected;
  }

  private void _onFolderTreeItemSelected(object? sender, ITreeItem e) {
    MainWindow.SlidePanels.ViewPager.SetCurrentItem(1, true);
  }
}