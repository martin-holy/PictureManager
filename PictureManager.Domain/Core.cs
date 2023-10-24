using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.Database;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Models;
using PictureManager.Domain.TreeCategories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain; 

public sealed class Core {
  private static Core _instance;
  private static readonly object _lock = new();
  public static Core Instance { get { lock (_lock) { return _instance ??= new(); } } }

  public static Db Db { get; } = new();
  public static Settings Settings { get; } = new();

  public static FoldersM FoldersM => Db.Folders.Model;
  public static GeoNamesM GeoNamesM => Db.GeoNames.Model;
  public static KeywordsM KeywordsM => Db.Keywords.Model;
  public static MediaItemsM MediaItemsM => Db.MediaItems.Model;
  public static PeopleM PeopleM => Db.People.Model;
  public static SegmentsM SegmentsM => Db.Segments.Model;
  public static VideoClipsM VideoClipsM => Db.VideoClips.Model;
  public static ViewersM ViewersM => Db.Viewers.Model;

  public static RatingsTreeCategory RatingsTreeCategory { get; } = new();

  public ImageComparerM ImageComparerM { get; } = new();
  public static TabControl MainTabs { get; } = new() { CanCloseTabs = true };
  public static MainWindowM MainWindowM { get; } = new();
  public static MediaItemsViews MediaItemsViews { get; } = new();
  public static MediaViewerM MediaViewerM { get; } = new();
  public static StatusPanelM StatusPanelM { get; } = new();
  public TitleProgressBarM TitleProgressBarM { get; } = new();
  public static ToolsTabsM ToolsTabsM { get; } = new() { CanCloseTabs = true };
  public static TreeViewCategoriesM TreeViewCategoriesM { get; } = new();

  public delegate Dictionary<string, string> FileOperationDeleteFunc(List<string> items, bool recycle, bool silent);
  public static FileOperationDeleteFunc FileOperationDelete { get; set; }
  public static Func<double> GetDisplayScale { get; set; }

  private Core() {
    Tasks.SetUiTaskScheduler();
    Settings.Load();
  }

  public Task InitAsync(IProgress<string> progress) {
    return Task.Run(() => {
      Db.AddDataAdapters();
      Drives.UpdateSerialNumbers();
      SimpleDB.Migrate(6, DatabaseMigration.Resolver);
      Db.LoadAllTables(progress);
      Db.LinkReferences(progress);
      Db.ClearDataAdapters();
      Db.RaiseReadyEvent();
      AttachEvents();
      progress.Report("Loading drives");
    });
  }

  public void AfterInit() {
    var scale = GetDisplayScale();
    MediaItemsViews.DefaultThumbScale = 1 / scale;
    SegmentsM.SetSegmentUiSize(scale);
    VideoClipsM.SetPlayer(MediaViewerM.MediaPlayerM);
    MediaItemsM.OnPropertyChanged(nameof(MediaItemsM.MediaItemsCount));

    KeywordsM.TreeCategory.AutoAddedGroup ??=
      Db.CategoryGroups.ItemCreate(KeywordsM.TreeCategory, "Auto Added");

    FoldersM.TreeCategory.AddDrives();
    Db.FolderKeywords.Reload();
    TreeViewCategoriesM.AddCategories();
    Db.CategoryGroups.AddCategory(PeopleM.TreeCategory);
    Db.CategoryGroups.AddCategory(KeywordsM.TreeCategory);
  }

  private void AttachEvents() {
    MainWindowM.PropertyChanged += (_, e) => {
      if (nameof(MainWindowM.IsFullScreen).Equals(e.PropertyName)) {
        var isFullScreen = MainWindowM.IsFullScreen;

        MediaViewerM.IsVisible = isFullScreen;

        if (!isFullScreen) {
          MediaItemsViews.Current?.SelectAndScrollToCurrentMediaItem();
          MediaViewerM.Deactivate();
        }

        TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      }
    };

    #region FoldersM EventHandlers

    Db.Folders.ItemRenamedEvent += (_, _) => {
      StatusPanelM.UpdateFilePath();
    };

    Db.Folders.ItemDeletedEvent += (_, e) => {
      Db.FavoriteFolders.ItemDeleteByFolder(e.Data);
      Db.MediaItems.ItemsDelete(e.Data.MediaItems.ToArray());
    };

    FoldersM.ItemCopiedEvent += (_, _) => {
      Db.FolderKeywords.Reload();
    };

    FoldersM.ItemMovedEvent += (_, _) => {
      Db.FolderKeywords.Reload();
      StatusPanelM.UpdateFilePath();
    };

    #endregion

    #region PeopleM EventHandlers

    Db.People.ItemRenamedEvent += (_, e) => {
      MediaItemsM.UpdateInfoBoxWithPerson(e.Data);
    };

    Db.People.ItemDeletedEvent  += (_, e) => {
      MediaItemsM.RemovePersonFromMediaItems(e.Data);
      Db.Segments.RemovePersonFromSegments(e.Data);
    };

    #endregion

    #region KeywordsM EventHandlers

    Db.Keywords.ItemRenamedEvent += (_, e) => {
      MediaItemsM.UpdateInfoBoxWithKeyword(e.Data);
    };

    Db.Keywords.ItemDeletedEvent += (_, e) => {
      Db.People.RemoveKeywordFromPeople(e.Data);
      Db.Segments.RemoveKeywordFromSegments(e.Data);
      MediaItemsM.RemoveKeywordFromMediaItems(e.Data);
    };

    #endregion

    #region MediaItemsM EventHandlers

    Db.MediaItems.ItemRenamedEvent += (_, _) => {
      MediaItemsViews.Current?.SoftLoad(MediaItemsViews.Current.FilteredItems, true, false);
    };

    Db.MediaItems.ItemDeletedEvent += (_, e) => {
      Db.Segments.ItemsDelete(e.Data.Segments);
    };

    Db.MediaItems.ItemsDeletedEvent += (_, e) => {
      MediaItemsViews.RemoveMediaItems(e.Data);

      if (MediaViewerM.IsVisible) {
        MediaViewerM.MediaItems.Remove(e.Data[0]);
        if (MediaItemsM.Current != null)
          MediaViewerM.Current = MediaItemsM.Current;
        else
          MainWindowM.IsFullScreen = false;
      }
    };

    MediaItemsM.MediaItemsOrientationChangedEventHandler += (_, e) => {
      if (MediaViewerM.IsVisible && e.Data.Contains(MediaItemsM.Current))
        MediaViewerM.OnPropertyChanged(nameof(MediaViewerM.Current));

      foreach (var __ in e.Data)
        MediaItemsM.DataAdapter.IsModified = true;

      MediaItemsViews.ReloadViewIfContains(e.Data);
    };

    MediaItemsM.MetadataChangedEventHandler += (_, _) => {
      TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      StatusPanelM.UpdateRating();
    };

    MediaItemsM.PropertyChanged += (_, e) => {
      if (nameof(MediaItemsM.Current).Equals(e.PropertyName)) {
        StatusPanelM.Update();

        if (MainWindowM.IsFullScreen)
          TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      }
    };

    #endregion

    MediaItemsViews.PropertyChanged += (_, e) => {
      if (nameof(MediaItemsViews.Current).Equals(e.PropertyName)) {
        TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
        MainWindowM.OnPropertyChanged(nameof(MainWindowM.CanOpenStatusPanel));
      }
    };

    MediaViewerM.PropertyChanged += (_, e) => {
      switch (e.PropertyName) {
        case nameof(MediaViewerM.IsVisible):
          StatusPanelM.UpdateFilePath();
          MainWindowM.OnPropertyChanged(nameof(MainWindowM.CanOpenStatusPanel));
          break;
        case nameof(MediaViewerM.Current):
          SegmentsM.SegmentsRectsM.MediaItem = MediaViewerM.Current;

          if (MediaViewerM.Current != null && MediaItemsM.Current != MediaViewerM.Current)
            MediaItemsM.Current = MediaViewerM.Current;
          break;
        case nameof(MediaViewerM.Scale):
          SegmentsM.SegmentsRectsM.UpdateScale(MediaViewerM.Scale);
          break;
      }
    };

    #region SegmentsM EventHandlers

    Db.Segments.SegmentPersonChangedEvent += (_, e) => {
      Db.People.OnSegmentPersonChanged(e.Data.Item1, e.Data.Item2, e.Data.Item3);
    };

    Db.Segments.SegmentsPersonChangedEvent += (_, e) => {
      Db.People.OnSegmentsPersonChanged(e.Data.Item1, e.Data.Item2, e.Data.Item3);
      PeopleM.PersonDetail?.ReloadIf(e.Data.Item2, e.Data.Item3);
      PeopleM.PeopleView?.ReGroupItems(e.Data.Item3?.Where(x => x.Segment != null).ToArray(), false);
      
      foreach (var mi in e.Data.Item2.Select(x => x.MediaItem).Distinct())
        mi.SetInfoBox();
    };

    Db.Segments.SegmentsKeywordsChangedEvent += (_, e) => {
      PeopleM.PersonDetail?.ReGroupIfContains(e.Data, false);
    };

    Db.Segments.ItemDeletedEvent += (_, e) => {
      Db.People.OnSegmentPersonChanged(e.Data, e.Data.Person, null);
      PeopleM.PersonDetail?.ReGroupIfContains(new[] { e.Data }, true);
    };

    #endregion

    #region MainTabs EventHandlers

    MainTabs.TabClosedEvent += (_, e) => {
      switch (e.Data.Data) {
        case MediaItemsView miView:
          MediaItemsViews.CloseView(miView);
          break;
        case PeopleM people:
          people.Selected.DeselectAll();
          break;
      }
    };

    MainTabs.PropertyChanged += (_, e) => {
      if (nameof(MainTabs.Selected).Equals(e.PropertyName))
        MediaItemsViews.SetCurrentView(MainTabs.Selected?.Data as MediaItemsView);
    };

    #endregion
  }

  public void SaveMetadataPrompt() {
    if (MediaItemsM.ModifiedItems.Count > 0 &&
        Dialog.Show(new MessageDialog(
          "Metadata Edit",
          "Some Media Items are modified, do you want to save them?",
          Res.IconQuestion,
          true)) == 1)
      MediaItemsM.SaveEdit();
  }

  public void SaveDBPrompt() {
    if (Db.Changes > 0 &&
        Dialog.Show(new MessageDialog(
          "Database changes",
          "There are some changes in database, do you want to save them?",
          Res.IconQuestion,
          true)) == 1)
      Db.SaveAllTables();
  }
}