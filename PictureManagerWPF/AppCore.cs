using System.Collections.Generic;
using System.Windows;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Properties;
using PictureManager.ShellStuff;
using PictureManager.ViewModels;
using MH.Utils.BaseClasses;
using MH.UI.WPF.Controls;
using PictureManager.Domain.Models;

namespace PictureManager {
  public sealed class AppCore : ObservableObject {
    public static double ScrollBarSize;
    public FoldersVM FoldersVM { get; }
    public MainWindowVM MainWindowVM { get; }
    public MainWindowContentVM MainWindowContentVM { get; }
    public MediaItemsVM MediaItemsVM { get; }
    public MediaViewerVM MediaViewerVM { get; }
    public PeopleVM PeopleVM { get; }
    public SegmentsVM SegmentsVM { get; }
    public ThumbnailsGridsVM ThumbnailsGridsVM { get; }
    public VideoClipsVM VideoClipsVM { get; }
    public PersonVM PersonVM { get; }
    public ViewerVM ViewerVM { get; }

    public TreeViewCategoriesVM TreeViewCategoriesVM { get; }

    public MainTabsVM MainTabsVM { get; }
    public ToolsTabsVM ToolsTabsVM { get; }

    public AppCore() {
      App.Core.CachePath = Settings.Default.CachePath;
      App.Core.ThumbnailSize = Settings.Default.ThumbnailSize;
      GeoNamesM.GeoNamesUserName = Settings.Default.GeoNamesUserName;
      Core.DialogHostShow = DialogHost.Show;
      MH.UI.WPF.Resources.Dictionaries.IconNameToBrush = ResourceDictionaries.Dictionaries.IconNameToBrush;

      MainWindowVM = new(App.Core, this);
      MainWindowContentVM = new();
      MainTabsVM = new();
      ToolsTabsVM = new();

      FoldersVM = new(App.Core, this, App.Core.FoldersM);
      MediaItemsVM = new(App.Core, this, App.Core.MediaItemsM);
      MediaViewerVM = new();
      PeopleVM = new(this, App.Core.PeopleM);
      SegmentsVM = new(App.Core, this, App.Core.SegmentsM);
      ThumbnailsGridsVM = new(App.Core, this, App.Core.ThumbnailsGridsM);
      VideoClipsVM = new(App.Core.VideoClipsM);

      TreeViewCategoriesVM = new(App.Core, this, App.Core.TreeViewCategoriesM);

      PersonVM = new(App.Core.PeopleM, App.Core.SegmentsM);
      ViewerVM = new(App.Core.ViewersM, App.Core.CategoryGroupsM);

      VideoClipsVM.VideoPlayer = MediaViewerVM.FullVideo;

      AttachEvents();
    }

    private void AttachEvents() {
      Settings.Default.PropertyChanged += (o, e) => {
        switch (e.PropertyName) {
          case nameof(Settings.Default.CachePath):
            App.Core.CachePath = Settings.Default.CachePath;
            break;
          case nameof(Settings.Default.ThumbnailSize):
            App.Core.ThumbnailSize = Settings.Default.ThumbnailSize;
            break;
          case nameof(Settings.Default.GeoNamesUserName):
            GeoNamesM.GeoNamesUserName = Settings.Default.GeoNamesUserName;
            break;
        }
      };

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

      App.Core.SegmentsM.SegmentDeletedEventHandler += (_, e) => {
        if (PersonVM.PersonM?.Equals(e.Data.Person) == true)
          PersonVM.ReloadPersonSegments();

        App.Core.SegmentsM.SegmentsDrawerRemove(e.Data);
      };

      App.Core.PeopleM.PeopleKeywordChangedEvent += (_, _) => {
        PersonVM.ReloadPersonSegments();
        App.Core.SegmentsM.Reload();
      };

      MainWindowVM.PropertyChanged += (_, e) => {
        if (nameof(MainWindowVM.IsFullScreen).Equals(e.PropertyName)) {
          var isFullScreen = MainWindowVM.IsFullScreen;

          App.Core.TreeViewCategoriesM.SetIsPinned(isFullScreen);
          MediaViewerVM.IsVisible = isFullScreen;

          if (!isFullScreen) {
            App.Core.ThumbnailsGridsM.Current?.ScrollToCurrentMediaItem();
            App.Core.TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
            MediaViewerVM.Deactivate();
            ToolsTabsVM.Deactivate(VideoClipsVM.ToolsTabsItem);
          }
        }
      };

      MediaViewerVM.PropertyChanged += (_, e) => {
        if (nameof(MediaViewerVM.IsVisible).Equals(e.PropertyName))
          App.Core.StatusPanelM.OnPropertyChanged(nameof(App.Core.StatusPanelM.FilePath));

        if (nameof(MediaViewerVM.Current).Equals(e.PropertyName))
          App.Core.SegmentsM.SegmentsRectsM.MediaItem = MediaViewerVM.Current;
      };

      MainTabsVM.TabClosedEventHandler += (_, e) => {
        if (e.Data.Content is ThumbnailsGridVM grid)
          ThumbnailsGridsVM.CloseGrid(grid);
      };

      MainTabsVM.PropertyChanged += async (_, e) => {
        if (nameof(MainTabsVM.Selected).Equals(e.PropertyName))
          await ThumbnailsGridsVM.SetCurrentGrid(MainTabsVM.Selected?.Content as ThumbnailsGridVM);
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
