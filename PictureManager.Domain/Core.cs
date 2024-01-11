using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.Database;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Extensions;
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
  public static VideoDetail VideoDetail { get; } = new();
  public static IPlatformSpecificUiMediaPlayer UiFullVideo { get; set; }
  public static IPlatformSpecificUiMediaPlayer UiDetailVideo { get; set; }
  public static SegmentsView SegmentsView { get; set; }

  public delegate Dictionary<string, string> FileOperationDeleteFunc(List<string> items, bool recycle, bool silent);
  public static FileOperationDeleteFunc FileOperationDelete { get; set; }
  public static Func<double> GetDisplayScale { get; set; }

  public RelayCommand<object> OpenSegmentsViewCommand { get; set; }

  private Core() {
    Tasks.SetUiTaskScheduler();
    Settings.Load();
  }

  public Task InitAsync(IProgress<string> progress) {
    return Task.Run(() => {
      Db.AddDataAdapters();
      Drives.UpdateSerialNumbers();
      progress.Report("Migrating Database");
      SimpleDB.Migrate(7, DatabaseMigration.Resolver);
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
    UiDetailVideo.SetModel(VideoDetail.MediaPlayer);

    OpenSegmentsViewCommand = new(OpenSegmentsView);
  }

  private void AttachEvents() {
    MainWindowM.PropertyChanged += (_, e) => {
      if (nameof(MainWindowM.IsInViewMode).Equals(e.PropertyName)) {
        var isInViewMode = MainWindowM.IsInViewMode;

        MediaViewerM.IsVisible = isInViewMode;

        if (isInViewMode) {
          UiDetailVideo.UnsetModel();
          VideoDetail.MediaPlayer.IsPlayOnOpened = true;
          UiFullVideo.SetModel(VideoDetail.MediaPlayer);
        }
        else {
          MediaItemsViews.SelectAndScrollToCurrentMediaItem();
          MediaViewerM.Deactivate();
          UiFullVideo.UnsetModel();
          VideoDetail.MediaPlayer.IsPlayOnOpened = false;
          UiDetailVideo.SetModel(VideoDetail.MediaPlayer);
        }

        TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      }
    };

    Settings.PropertyChanged += (_, e) => {
      if (nameof(Settings.GeoNamesUserName).Equals(e.PropertyName))
        GeoNamesM.ApiLimitExceeded = false;
    };

    VideoDetail.MediaPlayer.RepeatEndedEvent += delegate {
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
      Db.MediaItems.OnPersonRenamed(e.Data);
    };

    Db.People.ItemDeletedEvent += (_, e) => {
      Db.MediaItems.OnPersonDeleted(e.Data);
      Db.Segments.OnPersonDeleted(e.Data);
      PeopleM.PeopleView?.Remove(new[] { e.Data });
      PeopleM.PeopleToolsTabM?.Remove(new[] { e.Data });
      SegmentsView?.CvPeople.Remove(new[] { e.Data });
    };

    Db.People.KeywordsChangedEvent += items => {
      PeopleM.OnKeywordsChanged(items);
      PeopleM.PeopleToolsTabM?.Update(items);
      PeopleM.PeopleView?.Update(items);
      SegmentsView?.CvPeople.Update(items);
    };

    #endregion

    #region KeywordsM EventHandlers

    Db.Keywords.ItemRenamedEvent += (_, e) => {
      Db.MediaItems.OnKeywordRenamed(e.Data);
    };

    Db.Keywords.ItemDeletedEvent += (_, e) => {
      // TODO try to find generic code for the three below
      Db.People.OnKeywordDeleted(e.Data);
      Db.Segments.OnKeywordDeleted(e.Data);
      Db.MediaItems.OnKeywordDeleted(e.Data);
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
      VideoDetail.CurrentVideoItems.Remove(e.Data.OfType<VideoItemM>().ToArray());

      if (MediaViewerM.IsVisible) {
        MediaViewerM.MediaItems.Remove(e.Data[0]);
        if (MediaItemsM.Current != null)
          MediaViewerM.Current = MediaItemsM.Current;
        else
          MainWindowM.IsInViewMode = false;
      }
    };

    RotationDialogM.OrientationChangedEvent += items => {
      if (MediaViewerM.IsVisible && items.Contains(MediaItemsM.Current))
        MediaViewerM.OnPropertyChanged(nameof(MediaViewerM.Current));

      MediaItemsViews.ReWrapViews(items.Cast<MediaItemM>().ToArray());
    };

    Db.MediaItems.MetadataChangedEvent += items => {
      MediaItemsM.OnMetadataChanged(items);
      MediaItemsViews.UpdateViews(items);
      VideoDetail.CurrentVideoItems.Update(items.OfType<VideoItemM>().ToArray());
      TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      MediaItemsStatusBarM.UpdateRating();
    };

    MediaItemsM.PropertyChanged += (_, e) => {
      if (nameof(MediaItemsM.Current).Equals(e.PropertyName)) {
        MediaItemsStatusBarM.Update();
        VideoDetail.SetCurrent(MediaItemsM.Current);

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
          else
            VideoDetail.SetCurrent(MediaViewerM.Current, true);
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
      PeopleM.PersonDetail?.Update(e.Data.Item2);
      PeopleM.PeopleView?.Update(e.Data.Item3);
      Db.MediaItems.OnSegmentsPersonChanged(e.Data.Item2);
      SegmentsM.Selected.DeselectAll();

      // TODO is this all correct?
      if (SegmentsView != null) {
        SegmentsView.CvSegments.Update(e.Data.Item2);
        var pIn = e.Data.Item2.GetPeople().ToArray();
        var pOut = e.Data.Item3.Except(pIn).ToArray();
        SegmentsView.CvPeople.Update(pIn);
        SegmentsView.CvPeople.Remove(pOut);
      }
    };

    Db.Segments.KeywordsChangedEvent += items => {
      PeopleM.PersonDetail?.Update(items, true, false);
      Db.MediaItems.OnSegmentsKeywordsChanged(items);
      SegmentsView?.CvSegments.Update(items);
    };

    Db.Segments.ItemDeletedEvent += (_, e) => {
      Db.MediaItems.OnSegmentDeleted(e.Data);
      Db.People.OnSegmentPersonChanged(e.Data, e.Data.Person, null);
      PeopleM.PersonDetail?.Update(new[] { e.Data }, true, true);
      SegmentsView?.CvSegments.Remove(new[] { e.Data });
      SegmentsM.OnItemDeleted(e.Data);
    };

    Db.Segments.ItemsDeletedEvent += (_, e) => {
      Db.MediaItems.OnSegmentsDeleted(e.Data);
    };

    Db.Segments.ItemCreatedEvent += (_, e) => {
      Db.MediaItems.OnSegmentCreated(e.Data);
      SegmentsView?.CvSegments.Update(new[] { e.Data }, false);
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

  private static void OpenSegmentsView() {
    var result = SegmentsView.GetSegmentsToLoadUserInput();
    if (result < 1) return;
    var segments = SegmentsView.GetSegments(result).ToArray();

    if (SegmentsView == null) {
      SegmentsView = new(SegmentsM);
      PeopleM.AddEvents(SegmentsView.CvPeople);
    }

    MainTabs.Activate(Res.IconSegment, "Segments", SegmentsView);
    if (MediaViewerM.IsVisible) MainWindowM.IsInViewMode = false;
    SegmentsView.Reload(segments);
  }
}