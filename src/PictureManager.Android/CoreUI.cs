using Android.App;
using Android.Content;
using Android.OS;
using MH.Utils;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Android.Utils;
using PictureManager.Android.Views;
using PictureManager.Common;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.MediaItem;
using System.IO;
using System.Linq;

namespace PictureManager.Android;

public class CoreUI : ICoreP {
  private readonly string[] _mediaRoots = ["DCIM", "Pictures"];

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

  public void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) =>
    throw new System.NotImplementedException();

  public string GetFilePathCache(FolderM folder, string fileNameCache) =>
    IOExtensions.PathCombine(folder.FullPathCache, fileNameCache);

  public string GetFolderPathCache(FolderM folder) {
    var parts = folder.GetThisAndParents().Reverse().Select(x => x.Name).ToArray();
    var driveRoot = parts[0];
    var relativePath = string.Join(Path.DirectorySeparatorChar, parts.Skip(1));
    var cacheSubPath = Core.Settings.Common.CachePath.TrimStart(':').Replace('\\', Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

    return string.Concat(driveRoot, cacheSubPath, relativePath);
  }

  public bool IsFolderPathCacheableInMediaStore(FolderM folder) {
    if (Build.VERSION.SdkInt < BuildVersionCodes.Q) return true;

    var parts = folder.GetThisAndParents().Reverse().ToArray();    
    if (parts.Length > 1) {
      var driveRoot = parts[0].Name;
      var rootFolder = parts[1].Name;
      var deviceRoot = global::Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath;
      if (driveRoot.Equals(deviceRoot, System.StringComparison.OrdinalIgnoreCase) && _mediaRoots.Contains(rootFolder))
        return true;
    }

    return false;
  }

  private void _attachEvents() {
    Core.R.Folder.Tree.ItemSelectedEvent += _onFolderTreeItemSelected;
  }

  private void _onFolderTreeItemSelected(object? sender, ITreeItem e) {
    MainWindow.SlidePanels.ViewPager.SetCurrentItem(1, true);
  }
}