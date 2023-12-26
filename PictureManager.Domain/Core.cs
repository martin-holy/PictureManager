using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.Database;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
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
  public static ImagesM ImagesM => Db.Images.Model;
  public static KeywordsM KeywordsM => Db.Keywords.Model;
  public static MediaItemsM MediaItemsM => Db.MediaItems.Model;
  public static PeopleM PeopleM => Db.People.Model;
  public static SegmentsM SegmentsM => Db.Segments.Model;
  public static VideoClipsM VideoClipsM => Db.VideoClips.Model;
  public static VideosM VideosM => Db.Videos.Model;
  public static ViewersM ViewersM => Db.Viewers.Model;

  public static RatingsTreeCategory RatingsTreeCategory { get; } = new();

  public ImageComparerM ImageComparerM { get; } = new();
  public static TabControl MainTabs { get; } = new() { CanCloseTabs = true };
  public static MainWindowM MainWindowM { get; } = new();
  public static MediaItemsViews MediaItemsViews { get; } = new();
  public static MediaViewerM MediaViewerM { get; } = new();
  public static MediaItemsStatusBarM MediaItemsStatusBarM { get; } = new();
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
      Db.SetIsReady();
      AttachEvents();
      progress.Report("Loading UI");
    });
  }

  public void AfterInit() {
    var scale = GetDisplayScale();
    MediaItemsViews.DefaultThumbScale = 1 / scale;
    SegmentsM.SetSegmentUiSize(scale);
    MediaItemsM.UpdateItemsCount();

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
      if (nameof(MainWindowM.IsInViewMode).Equals(e.PropertyName)) {
        var isInViewMode = MainWindowM.IsInViewMode;

        MediaViewerM.IsVisible = isInViewMode;

        if (!isInViewMode) {
          MediaItemsViews.SelectAndScrollToCurrentMediaItem();
          MediaViewerM.Deactivate();
        }

        TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      }
    };


    VideosM.MediaPlayer.RepeatEndedEvent += delegate {
      MediaViewerM.OnPlayerRepeatEnded();
    };

    #region FoldersM EventHandlers

    Db.Folders.ItemRenamedEvent += (_, _) => {
      MediaItemsStatusBarM.UpdateFilePath();
    };

    Db.Folders.ItemDeletedEvent += (_, e) => {
      Db.FavoriteFolders.ItemDeleteByFolder(e.Data);
      Db.MediaItems.ItemsDelete(e.Data.MediaItems.Cast<MediaItemM>().ToArray());
    };

    FoldersM.ItemCopiedEvent += (_, _) => {
      Db.FolderKeywords.Reload();
    };

    FoldersM.ItemMovedEvent += (_, _) => {
      Db.FolderKeywords.Reload();
      MediaItemsStatusBarM.UpdateFilePath();
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
      Db.Segments.ItemsDelete(e.Data.Segments?.ToArray());
    };

    Db.MediaItems.ItemsDeletedEvent += (_, e) => {
      MediaItemsViews.RemoveMediaItems(e.Data);
      MediaItemsM.OnPropertyChanged(nameof(MediaItemsM.ModifiedItemsCount));

      if (MediaViewerM.IsVisible) {
        MediaViewerM.MediaItems.Remove(e.Data[0]);
        if (MediaItemsM.Current != null)
          MediaViewerM.Current = MediaItemsM.Current;
        else
          MainWindowM.IsInViewMode = false;
      }
    };

    MediaItemsM.MediaItemsOrientationChangedEvent += (_, e) => {
      if (MediaViewerM.IsVisible && e.Data.Contains(MediaItemsM.Current))
        MediaViewerM.OnPropertyChanged(nameof(MediaViewerM.Current));

      MediaItemsViews.ReWrapViewIfContains(e.Data);
    };

    MediaItemsM.MetadataChangedEvent += (_, e) => {
      MediaItemsViews.ReGroupViewIfContains(e.Data);
      MediaItemsM.OnPropertyChanged(nameof(MediaItemsM.ModifiedItemsCount));
      TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      MediaItemsStatusBarM.UpdateRating();
    };

    MediaItemsM.PropertyChanged += (_, e) => {
      if (nameof(MediaItemsM.Current).Equals(e.PropertyName)) {
        MediaItemsStatusBarM.Update();
        VideosM.ReloadCurrentVideoItems();

        if (MainWindowM.IsInViewMode)
          TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      }
    };

    #endregion

    MediaItemsViews.PropertyChanged += (_, e) => {
      if (nameof(MediaItemsViews.Current).Equals(e.PropertyName)) {
        TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
        MediaItemsStatusBarM.OnPropertyChanged(nameof(MediaItemsStatusBarM.IsVisible));
      }
    };

    MediaViewerM.PropertyChanged += (_, e) => {
      switch (e.PropertyName) {
        case nameof(MediaViewerM.IsVisible):
          MediaItemsStatusBarM.UpdateFilePath();
          MediaItemsStatusBarM.OnPropertyChanged(nameof(MediaItemsStatusBarM.IsVisible));
          break;
        case nameof(MediaViewerM.Current):
          SegmentsM.SegmentsRectsM.MediaItem = MediaViewerM.Current;
          MediaItemsStatusBarM.OnPropertyChanged(nameof(MediaItemsStatusBarM.FileSize));

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
      Db.MediaItems.Modify(e.Data.Item1.MediaItem);
    };

    Db.Segments.SegmentsPersonChangedEvent += (_, e) => {
      Db.People.OnSegmentsPersonChanged(e.Data.Item1, e.Data.Item2, e.Data.Item3);
      PeopleM.PersonDetail?.ReloadIf(e.Data.Item2);
      PeopleM.PeopleView?.Update(e.Data.Item3?.Where(x => x.Segment != null).ToArray());
      MediaItemsM.OnSegmentsPersonChanged(e.Data.Item2);
    };

    Db.Segments.SegmentsKeywordsChangedEvent += (_, e) => {
      PeopleM.PersonDetail?.ReGroupIfContains(e.Data, true, false);
      MediaItemsM.OnSegmentsKeywordsChanged(e.Data);
    };

    Db.Segments.ItemDeletedEvent += (_, e) => {
      Db.People.OnSegmentPersonChanged(e.Data, e.Data.Person, null);
      PeopleM.PersonDetail?.ReGroupIfContains(new[] { e.Data }, true, true);
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
        case Settings settings:
          settings.OnClosing();
          break;
      }
    };

    MainTabs.PropertyChanged += (_, e) => {
      if (nameof(MainTabs.Selected).Equals(e.PropertyName))
        MediaItemsViews.SetCurrentView(MainTabs.Selected?.Data as MediaItemsView);
    };

    #endregion

    ToolsTabsM.TabClosedEvent += (_, e) => {
      switch (e.Data.Data) {
        case PersonDetail personDetail:
          personDetail.Reload(null);
          break;
      }
    };
  }

  public void SaveDBPrompt() {
    if (Db.Changes > 0 &&
        Dialog.Show(new MessageDialog(
          "Database changes",
          "There are some changes in database. Do you want to save them?",
          Res.IconQuestion,
          true)) == 1)
      Db.SaveAllTables();
  }
}