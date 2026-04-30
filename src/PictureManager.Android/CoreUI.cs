using Android.Content;
using Android.OS;
using MH.UI.Android.Controls;
using MH.UI.Android.Controls.Hosts.DialogHost;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Android.Utils;
using PictureManager.Android.Views.Layout;
using PictureManager.Common;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using PictureManager.Common.Features.Segment;
using PictureManager.Common.Layout;
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
    MH.UI.Android.Utils.IconU.IconNameToColor = Resources.Res.IconToColorDic;
    MH.UI.Controls.CollectionView.ItemBorderSize = MH.UI.Android.Utils.DisplayU.DpToPx(3);
    MediaItemS.ReadMetadata = ViewModels.MediaItemVM.ReadMetadata;
    ImageS.WriteMetadata = (img, _) => ViewModels.MediaItemVM.WriteMetadata(img, _mainActivity);
    VideoVM.GetVideoMetadataFunc = MH.UI.Android.Utils.ImagingU.GetVideoMetadata;
    SegmentVM.SegmentSize = 80; // TODO move this to settings
    CoreVM.DisplayScale = 1.0 / DisplayU.Metrics.Density;
    SegmentS.ExportSegment = Utils.ImagingU.ExportSegment;
    DialogHost.Initialize(mainActivity, DialogFactory.GetDialog);
    AboutDialog.OpenUrl = _openUrl;
  }

  public void AfterInit(Context context) {
    ShareMediaItemsCommand = new(x => _shareMediaItems(Core.VM.GetActive<RealMediaItemM>(x)), Core.VM.AnyActive<RealMediaItemM>, Res.IconShare, "Share");
    _initSlidePanelsGrid(Core.VM.MainWindow.SlidePanelsGrid);
    _disableNotSupportedMainMenuItems(Core.VM.MainWindow.MainMenu);
    Core.VM.MainTabs.TabStrip = new(Dock.Top) { StartSlot = new MainTabsSlotVM() };
    Core.VM.Segment.Views.Tabs.TabStrip = new(Dock.Top) { StartSlot = new SegmentsViewsTabsSlotVM() };
    Core.VM.MainWindow.TreeViewCategories.TabStrip.StartSlot = Core.VM.MainWindow.MainMenu;
    Core.VM.MainWindow.ToolsTabs.TabStrip.StartSlot = Core.VM.MainWindow.MainMenu;
    MainWindow = new MainWindowV(context, Core.VM);
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

  // TODO currently not used. remove it?
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
    Core.VM.ToolsTabs.TabActivatedEvent += _onToolsTabsTabActivated;
    Core.VM.MediaItem.Views.CurrentViewSelectionChangedEvent += (_, _) => _updateMediaItemCommands();
    Core.VM.MediaItem.Bind(nameof(MediaItemVM.Current), x => x.Current, _ => _updateMediaItemCommands(), false);
    Core.VM.MainWindow.Bind(nameof(MainWindowVM.IsInViewMode), x => x.IsInViewMode, _onMainWindowIsInViewModeChanged, false);
  }

  private void _onMainTabsTabActivated(object? sender, IListItem e) {
    MainWindow.SlidePanels.ViewPager.SetCurrentItem(1, true);
  }

  private void _onToolsTabsTabActivated(object? sender, IListItem e) {
    if (e.Data is not VideoVM)
      MainWindow.SlidePanels.ViewPager.SetCurrentItem(2, true);
  }

  private void _onFolderTreeItemSelected(object? sender, ITreeItem e) {
    MainWindow.SlidePanels.ViewPager.SetCurrentItem(1, true);
  }

  private void _onMainWindowIsInViewModeChanged(bool isInViewMode) {
    _updateMediaItemCommands();
    MainWindow.SlidePanels.ViewPager.SetCurrentItem(1, true); // TODO this might be bad UX

    MainWindowV.UpdateTopAndBottomPanels(
      Core.VM.MainWindow.SlidePanelsGrid,
      isInViewMode,
      MainWindow.SlidePanels.ViewPager.GetCurrentIndex() == 1);

    if (isInViewMode)
      _mainActivity.Window!.EnterFullScreen(); // TODO bind to Core.VM.MainWindow.IsFullScreen and remove this
    else {
      Core.VM.Video.MediaPlayer.IsPlaying = false;
      _mainActivity.Window!.ExitFullScreen();
    }
  }

  private static void _initSlidePanelsGrid(SlidePanelsGrid grid) {
    // Left, Top, Right, Bottom, FullScreen (FullScreen is not part of SlidePanelsGrid)
    grid.PinLayouts = [
      [true, true, true, false, false], // browse mode
      [true, false, true, false, true]  // view mode
    ];
    grid.PanelTop.AutoCloseDelay = 3000;
    grid.PanelBottom.AutoCloseDelay = 3000;
    grid.ActiveLayout = 0;
  }

  private static void _disableNotSupportedMainMenuItems(MainMenuVM mm) {
    mm.HideMenuItems([
      CoreVM.CompressImagesCommand,
      CoreVM.ImagesToVideoCommand,
      CoreVM.ResizeSelectedImagesCommand,
      MediaItemsViewsVM.RebuildThumbnailsCommand,
      MediaItemsViewsVM.ViewModifiedCommand,
      CoreVM.ExportSegmentsCommand]);
  }

  public void Dispose() {
    if (_disposed) return;
    Core.R.Folder.Tree.ItemSelectedEvent -= _onFolderTreeItemSelected;
    Core.VM.MainTabs.TabActivatedEvent -= _onMainTabsTabActivated;
    Core.VM.ToolsTabs.TabActivatedEvent -= _onToolsTabsTabActivated;
    _disposed = true;
  }

  private Dictionary<string, string>? _fileOperationDelete(List<string> items, bool recycle, bool silent) {
    MediaStoreU.DeleteFiles(items, _mainActivity);
    return null;
  }

  private void _shareMediaItems(RealMediaItemM[] items) =>
    MediaStoreU.ShareFiles(_mainActivity, items.Select(x => x.FilePath));

  private void _openUrl(string url) {
    if (_mainActivity.PackageManager is not { } pm) return;
    if (global::Android.Net.Uri.Parse(url) is not { } uri) return;
    if (new Intent(Intent.ActionView, uri) is not { } intent) return;
    if (intent.ResolveActivity(pm) == null) return;
    _mainActivity.StartActivity(intent);
  }

  public IUiMediaPlayer CreatePlayer() => new AndroidMediaPlayer();
}