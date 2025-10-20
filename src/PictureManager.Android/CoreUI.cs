using Android.Content;
using Android.OS;
using MH.UI.Android.Utils;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Android.Utils;
using PictureManager.Android.Views;
using PictureManager.Common;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.MediaItem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Android;

public class CoreUI : ICoreP, IDisposable {
  private readonly MainActivity _mainActivity;
  private readonly string[] _mediaRoots = ["DCIM", "Pictures"];
  private bool _disposed;

  public MainWindowV MainWindow { get; private set; } = null!;
  public static RelayCommand<FolderM> ShareMediaItemsCommand { get; private set; } = null!;

  public CoreUI(MainActivity mainActivity) {
    _mainActivity = mainActivity;
    CoreR.FileOperationDelete = _fileOperationDelete;
    // TODO PORT
    MH.UI.Android.Utils.Init.Utils(mainActivity);
    MH.UI.Android.Utils.Init.SetDelegates();
    MH.UI.Android.Utils.Icons.IconNameToColor = Resources.Res.IconToColorDic;
    MH.UI.Controls.CollectionView.ItemBorderSize = MH.UI.Android.Utils.DisplayU.DpToPx(3);
    MediaItemS.ReadMetadata = ViewModels.MediaItemVM.ReadMetadata;
    MH.UI.Android.Controls.DialogHost.Initialize(mainActivity, DialogFactory.GetDialog);
  }

  public void AfterInit(Context context) {
    ShareMediaItemsCommand = new(x => _shareMediaItems(Core.VM.GetActive<RealMediaItemM>(x)), Core.VM.AnyActive<RealMediaItemM>, Res.IconShare, "Share");

    Core.VM.MainWindow.SlidePanelsGrid.PinLayouts = [
        [true, true, true, false, false], // browse mode
        [true, true, true, false, false]  // view mode
      ];
    Core.VM.MainWindow.SlidePanelsGrid.ActiveLayout = 0;
    MainWindow = new MainWindowV(context, Core.VM.MainWindow);
    _attachEvents();
  }

  private void _updateMediaItemCommands() {
    ShareMediaItemsCommand.RaiseCanExecuteChanged();
  }

  public void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) =>
    throw new NotImplementedException();

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
      if (driveRoot.Equals(deviceRoot, StringComparison.OrdinalIgnoreCase) && _mediaRoots.Contains(rootFolder))
        return true;
    }

    return false;
  }

  private void _attachEvents() {
    Core.R.Folder.Tree.ItemSelectedEvent += _onFolderTreeItemSelected;
    Core.VM.MainTabs.TabActivatedEvent += _onMainTabsTabActivated;
    Core.VM.MediaItem.Views.CurrentViewSelectionChangedEvent += (_, _) => _updateMediaItemCommands();
    this.Bind(Core.VM.MediaItem, x => x.Current, (t, _) => t._updateMediaItemCommands(), false);
    this.Bind(Core.VM.MainWindow, x => x.IsInViewMode, (t, _) => t._updateMediaItemCommands(), false);
  }

  private void _onMainTabsTabActivated(object? sender, IListItem e) {
    MainWindow.SlidePanels.ViewPager.SetCurrentItem(1, true);
  }

  private void _onFolderTreeItemSelected(object? sender, ITreeItem e) {
    MainWindow.SlidePanels.ViewPager.SetCurrentItem(1, true);
  }

  public void Dispose() {
    if (_disposed) return;
    Core.R.Folder.Tree.ItemSelectedEvent -= _onFolderTreeItemSelected;
    Core.VM.MainTabs.TabActivatedEvent -= _onMainTabsTabActivated;
    _disposed = true;
  }

  private Dictionary<string, string>? _fileOperationDelete(List<string> items, bool recycle, bool silent) {
    MediaStoreU.DeleteFiles(items, _mainActivity);
    return null;
  }

  private void _shareMediaItems(RealMediaItemM[] items) =>
    MediaStoreU.ShareFiles(_mainActivity, items.Select(x => x.FilePath));
}