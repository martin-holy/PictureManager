using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Database;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.TreeCategories;
using System;
using System.Collections.Generic;
using System.IO;
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
  public static VideoThumbsM VideoThumbsM { get; } = new();
  public static IPlatformSpecificUiMediaPlayer UiFullVideo { get; set; }
  public static IPlatformSpecificUiMediaPlayer UiDetailVideo { get; set; }
  public static IVideoFrameSaver VideoFrameSaver { get; set; }
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
    AttachCategoryGroupsEventHandlers();
    AttachFoldersEventHandlers();
    AttachGeoLocationsEventHandlers();
    AttachGeoNamesEventHandlers();
    AttachKeywordsEventHandlers();
    AttachMediaItemGeoLocationEventHandlers();
    AttachMediaItemsEventHandlers();
    AttachPeopleEventHandlers();
    AttachSegmentsEventHandlers();

    MainWindowM.PropertyChanged += (_, e) => {
      if (nameof(MainWindowM.IsInViewMode).Equals(e.PropertyName)) {
        var isInViewMode = MainWindowM.IsInViewMode;

        MediaViewerM.IsVisible = isInViewMode;

        if (isInViewMode) {
          UiDetailVideo.UnsetModel();
          VideoDetail.MediaPlayer.IsPlayOnOpened = true;
          UiFullVideo.SetModel(VideoDetail.MediaPlayer);
          SegmentsM.SegmentsRectsM.MediaItem = MediaItemsM.Current;
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

    VideoDetail.CurrentVideoItems.Selected.ItemsChangedEventHandler += (_, e) => {
      var vi = e.Data.FirstOrDefault();
      MediaItemsM.Current = (MediaItemM)vi ?? VideoDetail.Current;
      VideoDetail.MediaPlayer.SetCurrent(vi);
    };

    MediaViewerM.PropertyChanged += (_, e) => {
      switch (e.PropertyName) {
        case nameof(MediaViewerM.IsVisible):
          MediaItemsStatusBarM.UpdateFilePath();
          MediaItemsStatusBarM.OnPropertyChanged(nameof(MediaItemsStatusBarM.IsVisible));
          break;
        case nameof(MediaViewerM.Current):
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

    ToolsTabsM.TabClosedEvent += (_, e) => {
      switch (e.Data.Data) {
        case PersonDetail personDetail:
          personDetail.Reload(null);
          break;
      }
    };
  }

  private static void AttachCategoryGroupsEventHandlers() =>
    Db.CategoryGroups.ItemDeletedEvent += (_, e) => {
      Db.Keywords.MoveGroupItemsToRoot(e.Data);
      Db.People.MoveGroupItemsToRoot(e.Data);
    };

  private static void AttachFoldersEventHandlers() {
    Db.Folders.ItemCreatedEvent += (_, e) =>
      Db.FolderKeywords.LoadIfContains((FolderM)e.Data.Parent);

    Db.Folders.ItemRenamedEvent += (_, e) => {
      Db.FolderKeywords.LoadIfContains(e.Data);
      MediaItemsStatusBarM.UpdateFilePath();
    };

    Db.Folders.ItemDeletedEvent += (_, e) => {
      Db.FavoriteFolders.ItemDeleteByFolder(e.Data);
      Db.MediaItems.ItemsDelete(e.Data.MediaItems.Cast<MediaItemM>().ToArray());
      FoldersM.DeleteFromDisk(e.Data);
    };

    Db.Folders.ItemsDeletedEvent += (_, _) =>
      Db.FolderKeywords.Reload();

    FoldersM.ItemCopiedEvent += (_, _) =>
      Db.FolderKeywords.Reload();

    FoldersM.ItemMovedEvent += (_, _) => {
      Db.FolderKeywords.Reload();
      MediaItemsStatusBarM.UpdateFilePath();
    };
  }

  private static void AttachGeoLocationsEventHandlers() {
    Db.GeoLocations.ItemUpdatedEvent += (_, e) => {
      foreach (var kv in Db.MediaItemGeoLocation.All.Where(x => ReferenceEquals(x.Value, e.Data)))
        Db.MediaItems.Modify(kv.Key);
    };
  }

  private static void AttachGeoNamesEventHandlers() {
    Db.GeoNames.ItemDeletedEvent += (_, e) =>
      Db.GeoLocations.RemoveGeoName(e.Data);
  }

  private static void AttachKeywordsEventHandlers() {
    Db.Keywords.ItemRenamedEvent += (_, e) =>
      Db.MediaItems.ModifyIfContains(e.Data);

    Db.Keywords.ItemDeletedEvent += (_, e) => {
      Db.People.RemoveKeyword(e.Data);
      Db.Segments.RemoveKeyword(e.Data);
      Db.MediaItems.RemoveKeyword(e.Data);
    };
  }

  private static void AttachMediaItemGeoLocationEventHandlers() {
    Db.MediaItemGeoLocation.ItemCreatedEvent += (_, e) => Db.MediaItems.Modify(e.Data.Key);
    Db.MediaItemGeoLocation.ItemDeletedEvent += (_, e) => Db.MediaItems.Modify(e.Data.Key);
  }

  private static void AttachMediaItemsEventHandlers() {
    Db.MediaItems.ItemRenamedEvent += (_, _) =>
      MediaItemsViews.Current?.SoftLoad(MediaItemsViews.Current.FilteredItems, true, false);

    Db.MediaItems.MetadataChangedEvent += items => {
      MediaItemsM.OnMetadataChanged(items);
      MediaItemsViews.UpdateViews(items);
      VideoDetail.CurrentVideoItems.Update(items.OfType<VideoItemM>().ToArray());
      TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      MediaItemsStatusBarM.UpdateRating();
    };

    Db.MediaItems.OrientationChangedEvent += items => {
      if (MediaViewerM.IsVisible && items.Contains(MediaItemsM.Current))
        MediaViewerM.OnPropertyChanged(nameof(MediaViewerM.Current));

      MediaItemsViews.ReWrapViews(items.Cast<MediaItemM>().ToArray());
    };

    Db.MediaItems.ItemDeletedEvent += (_, e) =>
      Db.Segments.ItemsDelete(e.Data.Segments?.ToArray());

    Db.MediaItems.ItemsDeletedEvent += (_, e) => {
      MediaItemsM.Current = MediaViewerM.IsVisible && e.Data.All(x => x is RealMediaItemM)
        ? MediaViewerM.MediaItems.GetNextOrPreviousItem(e.Data)
        : e.Data.OfType<VideoItemM>().FirstOrDefault()?.Video;

      MediaItemsViews.RemoveMediaItems(e.Data);
      VideoDetail.CurrentVideoItems.Remove(e.Data.OfType<VideoItemM>().ToArray());

      if (MediaViewerM.IsVisible) {
        MediaViewerM.MediaItems.Remove(e.Data[0]);
        if (MediaItemsM.Current != null)
          MediaViewerM.Current = MediaItemsM.Current;
        else
          MainWindowM.IsInViewMode = false;
      }

      FileOperationDelete(e.Data.OfType<RealMediaItemM>().Select(x => x.FilePath).Where(File.Exists).ToList(), true, false);
    };

    MediaItemsM.PropertyChanged += (_, e) => {
      if (nameof(MediaItemsM.Current).Equals(e.PropertyName)) {
        MediaItemsStatusBarM.Update();
        VideoDetail.SetCurrent(MediaItemsM.Current);

        if (MainWindowM.IsInViewMode) {
          TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
          SegmentsM.SegmentsRectsM.MediaItem = MediaItemsM.Current;
        }
      }
    };

    MediaItemsViews.PropertyChanged += (_, e) => {
      if (nameof(MediaItemsViews.Current).Equals(e.PropertyName)) {
        TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
        MediaItemsStatusBarM.OnPropertyChanged(nameof(MediaItemsStatusBarM.IsVisible));
      }
    };
  }

  private static void AttachPeopleEventHandlers() {
    Db.People.ItemRenamedEvent += (_, e) =>
      Db.MediaItems.ModifyIfContains(e.Data);

    Db.People.KeywordsChangedEvent += items => {
      PeopleM.PersonDetail?.UpdateDisplayKeywordsIfContains(items);
      PeopleM.PeopleToolsTabM?.Update(items);
      PeopleM.PeopleView?.Update(items);
      SegmentsView?.CvPeople.Update(items);
    };

    Db.People.ItemDeletedEvent += (_, e) => {
      Db.MediaItems.RemovePerson(e.Data);
      Db.Segments.RemovePerson(e.Data);
      PeopleM.Selected.Set(e.Data, false);
      PeopleM.PeopleView?.Remove(e.Data);
      PeopleM.PeopleToolsTabM?.Remove(e.Data);
      SegmentsView?.CvPeople.Remove(e.Data);

      if (ReferenceEquals(PeopleM.PersonDetail?.PersonM, e.Data))
        ToolsTabsM.Close(PeopleM.PersonDetail);
    };
  }

  private static void AttachSegmentsEventHandlers() {
    Db.Segments.ItemCreatedEvent += (_, e) => {
      Db.MediaItems.AddSegment(e.Data);
      SegmentsView?.CvSegments.Update(e.Data, false);
    };

    Db.Segments.SegmentPersonChangedEvent += (_, e) => {
      Db.People.OnSegmentPersonChanged(e.Data.Item1, e.Data.Item2, e.Data.Item3);
    };

    Db.Segments.SegmentsPersonChangedEvent += (_, e) => {
      Db.People.OnSegmentsPersonChanged(e.Data.Item1, e.Data.Item2, e.Data.Item3);
      Db.MediaItems.TogglePerson(e.Data.Item2);
      PeopleM.PersonDetail?.Update(e.Data.Item2);
      PeopleM.PeopleView?.Update(e.Data.Item3);
      SegmentsM.Selected.DeselectAll();

      if (SegmentsView != null) {
        SegmentsView.CvSegments.Update(e.Data.Item2, false);
        var pIn = e.Data.Item2.GetPeople().ToArray();
        var pOut = e.Data.Item3.Except(pIn).ToArray();
        SegmentsView.CvPeople.Update(pIn, false);
        SegmentsView.CvPeople.Remove(pOut);
      }
    };

    Db.Segments.KeywordsChangedEvent += items => {
      Db.MediaItems.ModifyIfContains(items);
      PeopleM.PersonDetail?.Update(items, true, false);
      SegmentsView?.CvSegments.Update(items);
    };

    Db.Segments.ItemDeletedEvent += (_, e) => {
      Db.People.OnSegmentPersonChanged(e.Data, e.Data.Person, null);
      SegmentsM.Selected.Set(e.Data, false);
    };

    Db.Segments.ItemsDeletedEvent += (_, e) => {
      Db.MediaItems.RemoveSegments(e.Data);
      PeopleM.PersonDetail?.Update(e.Data.ToArray(), true, true);
      SegmentsView?.CvSegments.Remove(e.Data.ToArray());
      SegmentsM.SegmentsDrawerM.RemoveIfContains(e.Data.ToArray());
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
    SegmentsView ??= new(SegmentsM);
    MainTabs.Activate(Res.IconSegment, "Segments", SegmentsView);
    if (MediaViewerM.IsVisible) MainWindowM.IsInViewMode = false;
    SegmentsView.Reload(segments);
  }
}