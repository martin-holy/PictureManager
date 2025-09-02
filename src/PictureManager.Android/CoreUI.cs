using Android.App;
using Android.Content;
using MH.Utils.Interfaces;
using PictureManager.Android.Utils;
using PictureManager.Android.Views;
using PictureManager.Common;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Android;

public class CoreUI : ICoreP {
  public MainWindowV MainWindow { get; private set; } = null!;

  public CoreUI(MainActivity mainActivity) {
    // TODO PORT
    MH.UI.Android.Utils.Init.SetDelegates();
    MH.UI.Android.Utils.Icons.IconNameToColor = Resources.Res.IconToColorDic;
    MH.UI.Android.Utils.DisplayU.Init(Application.Context!.Resources!.DisplayMetrics!);
    MH.UI.Controls.CollectionView.ItemBorderSize = MH.UI.Android.Utils.DisplayU.DpToPx(3);
    MediaItemS.ReadMetadata = ViewModels.MediaItemVM.ReadMetadata;
    MH.UI.Android.Controls.DialogHost.Initialize(mainActivity, DialogFactory.GetDialog);
  }

  public void AfterInit(Context context) {
    Core.Settings.MediaItem.MediaItemThumbScale = 0.5;
    MainWindow = new MainWindowV(context, Core.VM.MainWindow);
    _attachEvents();
  }

  public void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) => throw new System.NotImplementedException();
  public string GetFilePathCache(FolderM folder, string fileNameCache) => throw new System.NotImplementedException();
  public string GetFolderPathCache(FolderM folder) => string.Empty; // TODO PORT

  private void _attachEvents() {
    Core.R.Folder.Tree.ItemSelectedEvent += _onFolderTreeItemSelected;
  }

  private void _onFolderTreeItemSelected(object? sender, ITreeItem e) {
    MainWindow.SlidePanels.ViewPager.SetCurrentItem(1, true);
  }
}