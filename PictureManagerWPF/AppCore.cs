using System.Collections.Generic;
using System.Windows;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Properties;
using PictureManager.ShellStuff;
using PictureManager.ViewModels;
using PictureManager.ViewModels.Tree;
using MH.Utils.BaseClasses;

namespace PictureManager {
  public sealed class AppCore : ObservableObject {
    public static double ScrollBarSize;
    public MainWindowVM MainWindowVM { get; }
    public MainWindowContentVM MainWindowContentVM { get; }
    public MediaItemsVM MediaItemsVM { get; }
    public MediaViewerVM MediaViewerVM { get; }
    public PeopleVM PeopleVM { get; }
    public SegmentsVM SegmentsVM { get; }
    public ThumbnailsGridsVM ThumbnailsGridsVM { get; }
    public VideoClipsVM VideoClipsVM { get; }
    public ViewersVM ViewersVM { get; }
    public PersonVM PersonVM { get; }
    public ViewerVM ViewerVM { get; }
    public VideoClipsTreeVM VideoClipsTreeVM { get; }

    public TreeViewCategoriesVM TreeViewCategoriesVM { get; }

    public MainTabsVM MainTabsVM { get; }
    public ToolsTabsVM ToolsTabsVM { get; }
    public StatusPanelVM StatusPanelVM { get; }

    public AppCore() {
      App.Core.CachePath = Settings.Default.CachePath;
      App.Core.ThumbnailSize = Settings.Default.ThumbnailSize;

      MainWindowVM = new(App.Core, this);
      MainWindowContentVM = new();
      MainTabsVM = new();
      ToolsTabsVM = new();

      MediaItemsVM = new(App.Core, this, App.Core.MediaItemsM);
      MediaViewerVM = new();
      PeopleVM = new(this, App.Core.PeopleM);
      SegmentsVM = new(App.Core, this, App.Core.SegmentsM);
      ThumbnailsGridsVM = new(App.Core, this, App.Core.ThumbnailsGridsM);
      VideoClipsTreeVM = new(App.Core.VideoClipsM);
      VideoClipsVM = new(App.Core.VideoClipsM, VideoClipsTreeVM);
      ViewersVM = new(this, App.Core.ViewersM);

      TreeViewCategoriesVM = new(App.Core, this);

      StatusPanelVM = new(App.Core);
      PersonVM = new(App.Core.PeopleM, App.Core.SegmentsM);
      ViewerVM = new(App.Core.ViewersM, App.Core.CategoryGroupsM, TreeViewCategoriesVM.TvCategories);

      ViewersVM.SetCurrent(null);
      VideoClipsVM.VideoPlayer = MediaViewerVM.FullVideo;

      AttachEvents();
    }

    private void AttachEvents() {
      App.Core.SegmentsM.SegmentsPersonChangedEvent += (_, _) => {
        PersonVM.ReloadPersonSegments();
        App.Core.SegmentsM.Reload();

        if (MediaViewerVM.IsVisible)
          MediaViewerVM.Current?.SetInfoBox();
      };

      App.Core.SegmentsM.SegmentsKeywordChangedEvent += (_, _) => {
        PersonVM.ReloadPersonSegments();
        App.Core.SegmentsM.Reload();
      };

      App.Core.PeopleM.PeopleKeywordChangedEvent += (_, _) => {
        PersonVM.ReloadPersonSegments();
        App.Core.SegmentsM.Reload();
      };

      MainWindowVM.PropertyChanged += (_, e) => {
        if (nameof(MainWindowVM.IsFullScreen).Equals(e.PropertyName)) {
          var isFullScreen = MainWindowVM.IsFullScreen;

          TreeViewCategoriesVM.SetIsPinned(isFullScreen);
          MediaViewerVM.IsVisible = isFullScreen;

          if (!isFullScreen) {
            ThumbnailsGridsVM.ScrollToCurrentMediaItem();
            TreeViewCategoriesVM.MarkUsedKeywordsAndPeople();
            MediaViewerVM.Deactivate();
            ToolsTabsVM.Deactivate(VideoClipsVM.ToolsTabsItem);
          }
        }
      };

      MediaViewerVM.PropertyChanged += (_, e) => {
        if (nameof(MediaViewerVM.IsVisible).Equals(e.PropertyName))
          StatusPanelVM.OnPropertyChanged(nameof(StatusPanelVM.FilePath));

        if (nameof(MediaViewerVM.Current).Equals(e.PropertyName))
          App.Core.SegmentsM.SegmentsRectsM.MediaItem = MediaViewerVM.Current;
      };

      MainTabsVM.TabClosedEvent += (_, e) => {
        if (e.Data is HeaderedListItem<object, string> { Content: ThumbnailsGridVM grid })
          ThumbnailsGridsVM.CloseGrid(grid);
      };

      MainTabsVM.PropertyChanged += async (_, e) => {
        if (nameof(MainTabsVM.Selected).Equals(e.PropertyName)) {
          await ThumbnailsGridsVM.SetCurrentGrid(MainTabsVM.Selected?.Content as ThumbnailsGridVM);
          TreeViewCategoriesVM.MarkUsedKeywordsAndPeople();
        }
      };
    }

    public static CollisionResult ShowFileOperationCollisionDialog(string srcFilePath, string destFilePath, Window owner, ref string fileName) {
      var result = CollisionResult.Skip;
      var outFileName = fileName;
      var srcMi = App.Core.FoldersM.GetMediaItemByPath(srcFilePath);
      var destMi = App.Core.FoldersM.GetMediaItemByPath(destFilePath);

      Core.RunOnUiThread(() => {
        srcMi?.SetThumbSize();
        srcMi?.SetInfoBox();
        destMi?.SetThumbSize();
        destMi?.SetInfoBox();

        var cd = new FileOperationCollisionDialog(srcFilePath, destFilePath, srcMi, destMi, owner);
        cd.ShowDialog();
        result = cd.Result;
        outFileName = cd.FileName;
      }).GetAwaiter().GetResult();

      fileName = outFileName;

      return result;
    }

    public static Dictionary<string, string> FileOperationDelete(List<string> items, bool recycle, bool silent) {
      var fops = new PicFileOperationProgressSink();
      using var fo = new FileOperation(fops);
      fo.SetOperationFlags(
        (recycle ? FileOperationFlags.FOFX_RECYCLEONDELETE : FileOperationFlags.FOF_WANTNUKEWARNING) |
        (silent
          ? FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION |
            FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOFX_KEEPNEWERFILE
          : FileOperationFlags.FOF_NOCONFIRMMKDIR));

      foreach (var x in items)
        fo.DeleteItem(x);
      fo.PerformOperations();

      return fops.FileOperationResult;
    }
  }
}
