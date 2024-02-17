using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Database;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.TreeCategories;
using PictureManager.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain;

public sealed class Core {
  private static Core _inst;
  private static readonly object _lock = new();
  public static Core Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public static Db Db { get; } = new();
  public static Settings Settings { get; } = new();

  public static FoldersM FoldersM => Db.Folders.Model;
  public static KeywordsM KeywordsM => Db.Keywords.Model;
  public static PeopleM PeopleM => Db.People.Model;
  public static SegmentsM SegmentsM => Db.Segments.Model;
  public static ViewersM ViewersM => Db.Viewers.Model;

  public static RatingsTreeCategory RatingsTreeCategory { get; } = new();

  public ImageComparerM ImageComparerM { get; } = new();
  public static TabControl MainTabs { get; } = new() { CanCloseTabs = true };
  public static MediaItemsViews MediaItemsViews { get; } = new();
  public static MediaViewerM MediaViewerM { get; } = new();
  public static MediaItemsStatusBarM MediaItemsStatusBarM { get; } = new();
  public TitleProgressBarM TitleProgressBarM { get; } = new();
  public static TreeViewCategoriesM TreeViewCategoriesM { get; } = new();
  public static VideoDetail VideoDetail { get; } = new();
  public static VideoThumbsM VideoThumbsM { get; } = new();
  public static IPlatformSpecificUiMediaPlayer UiFullVideo { get; set; }
  public static IPlatformSpecificUiMediaPlayer UiDetailVideo { get; set; }
  public static IVideoFrameSaver VideoFrameSaver { get; set; }
  public static SegmentsView SegmentsView { get; set; }

  public static CoreM M { get; private set; }
  public static CoreVM VM { get; private set; }

  public delegate Dictionary<string, string> FileOperationDeleteFunc(List<string> items, bool recycle, bool silent);
  public static FileOperationDeleteFunc FileOperationDelete { get; set; }
  public static Func<double> GetDisplayScale { get; set; }

  public static RelayCommand OpenSegmentsViewCommand { get; set; }

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
    M = new(Db);
    VM = new(M, Db);
    AttachVMEvents();

    KeywordsM.TreeCategory.AutoAddedGroup ??=
      Db.CategoryGroups.ItemCreate(KeywordsM.TreeCategory, "Auto Added");

    FoldersM.TreeCategory.AddDrives();
    ViewersM.SetCurrent(ViewersM.Current);
    ViewersM.Current?.UpdateHashSets();
    TreeViewCategoriesM.AddCategories();
    Db.CategoryGroups.AddCategory(PeopleM.TreeCategory);
    Db.CategoryGroups.AddCategory(KeywordsM.TreeCategory);
    VideoDetail.MediaPlayer.SetView(UiFullVideo);
    VideoDetail.MediaPlayer.SetView(UiDetailVideo);
    OpenSegmentsViewCommand = new(OpenSegmentsView, Res.IconSegment, "Segments View");
  }

  private void AttachVMEvents() {
    AttachVMMediaItemsEventHandlers();

    VM.MainWindow.PropertyChanged += (_, e) => {
      if (nameof(VM.MainWindow.IsInViewMode).Equals(e.PropertyName)) {
        var isInViewMode = VM.MainWindow.IsInViewMode;

        MediaViewerM.IsVisible = isInViewMode;

        if (isInViewMode) {
          VideoDetail.MediaPlayer.SetView(UiFullVideo);
          SegmentsM.SegmentsRectsM.MediaItem = VM.MediaItems.Current;
        }
        else {
          MediaItemsViews.SelectAndScrollToCurrentMediaItem();
          MediaViewerM.Deactivate();
          VideoDetail.MediaPlayer.SetView(UiDetailVideo);
        }

        TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      }
    };
  }

  private void AttachVMMediaItemsEventHandlers() {
    VM.MediaItems.PropertyChanged += (_, e) => {
      if (nameof(VM.MediaItems.Current).Equals(e.PropertyName)) {
        MediaItemsStatusBarM.Update();
        VideoDetail.SetCurrent(VM.MediaItems.Current);

        if (VM.MainWindow.IsInViewMode) {
          TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
          SegmentsM.SegmentsRectsM.MediaItem = VM.MediaItems.Current;
        }
      }
    };
  }

  private void AttachEvents() {
    AttachCategoryGroupsEventHandlers();
    AttachFoldersEventHandlers();
    AttachGeoLocationsEventHandlers();
    AttachGeoNamesEventHandlers();
    AttachKeywordsEventHandlers();
    AttachMediaItemsEventHandlers();
    AttachPeopleEventHandlers();
    AttachSegmentsEventHandlers();

    Settings.PropertyChanged += (_, e) => {
      if (nameof(Settings.GeoNamesUserName).Equals(e.PropertyName))
        Db.GeoNames.ApiLimitExceeded = false;
    };

    VideoDetail.MediaPlayer.RepeatEndedEvent += delegate {
      MediaViewerM.OnPlayerRepeatEnded();
    };

    VideoDetail.CurrentVideoItems.Selected.ItemsChangedEventHandler += (_, e) => {
      var vi = e.Data.FirstOrDefault();
      VM.MediaItems.Current = (MediaItemM)vi ?? VideoDetail.Current;
      VideoDetail.MediaPlayer.SetCurrent(vi);
    };

    MediaViewerM.PropertyChanged += (_, e) => {
      switch (e.PropertyName) {
        case nameof(MediaViewerM.IsVisible):
          MediaItemsStatusBarM.Update();
          MediaItemsStatusBarM.OnPropertyChanged(nameof(MediaItemsStatusBarM.IsVisible));
          break;
        case nameof(MediaViewerM.Current):
          if (MediaViewerM.Current != null && !ReferenceEquals(VM.MediaItems.Current, MediaViewerM.Current))
            VM.MediaItems.Current = MediaViewerM.Current;
          else
            VideoDetail.SetCurrent(MediaViewerM.Current, true);
          break;
        case nameof(MediaViewerM.Scale):
          SegmentsM.SegmentsRectsM.UpdateScale(MediaViewerM.Scale);
          break;
      }
    };

    MainTabs.TabClosedEvent += tab => {
      switch (tab.Data) {
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
    Db.GeoLocations.ItemUpdatedEvent += (_, e) =>
      Db.MediaItems.ModifyIfContains(e.Data);

    Db.GeoLocations.ItemDeletedEvent += (_, e) =>
      Db.MediaItems.ModifyIfContains(e.Data);
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

  private static void AttachMediaItemsEventHandlers() {
    Db.MediaItems.ItemCreatedEvent += (_, _) =>
      VM.UpdateMediaItemsCount();

    Db.MediaItems.ItemRenamedEvent += (_, _) => {
      VM.MediaItems.OnPropertyChanged(nameof(VM.MediaItems.Current));
      MediaItemsViews.Current?.SoftLoad(MediaItemsViews.Current.FilteredItems, true, false);
    };

    Db.MediaItems.MetadataChangedEvent += items => {
      var all = items.OfType<VideoItemM>().Select(x => x.Video).Concat(items).Distinct().ToArray();
      VM.MediaItems.OnMetadataChanged(all);
      MediaItemsViews.UpdateViews(all);
      VideoDetail.CurrentVideoItems.Update(items.OfType<VideoItemM>().ToArray());
      TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      MediaItemsStatusBarM.UpdateRating();
      VM.UpdateModifiedMediaItemsCount();
    };

    Db.MediaItems.OrientationChangedEvent += items => {
      foreach (var rmi in items) {
        rmi.SetThumbSize(true);
        File.Delete(rmi.FilePathCache);
      }

      if (MediaViewerM.IsVisible && items.Contains(MediaViewerM.Current))
        MediaViewerM.Current = MediaViewerM.Current;

      MediaItemsViews.ReWrapViews(items.Cast<MediaItemM>().ToArray());
      if (items.Contains(VideoDetail.Current))
        VideoDetail.CurrentVideoItems.ReWrapAll();
    };

    Db.MediaItems.ItemDeletedEvent += (_, e) => {
      Db.Segments.ItemsDelete(e.Data.Segments?.ToArray());
      if (e.Data.GeoLocation != null)
        Db.MediaItemGeoLocation.IsModified = true;
    };

    Db.MediaItems.ItemsDeletedEvent += (_, e) => {
      VM.MediaItems.Current = MediaViewerM.IsVisible && e.Data.All(x => x is RealMediaItemM)
        ? MediaViewerM.MediaItems.GetNextOrPreviousItem(e.Data)
        : e.Data.OfType<VideoItemM>().FirstOrDefault()?.Video;

      VM.UpdateMediaItemsCount();
      VM.UpdateModifiedMediaItemsCount();
      MediaItemsViews.RemoveMediaItems(e.Data);
      VideoDetail.CurrentVideoItems.Remove(e.Data.OfType<VideoItemM>().ToArray());

      if (MediaViewerM.IsVisible) {
        MediaViewerM.MediaItems.Remove(e.Data[0]);
        if (VM.MediaItems.Current != null)
          MediaViewerM.Current = VM.MediaItems.Current;
        else
          VM.MainWindow.IsInViewMode = false;
      }
      
      FileOperationDelete(e.Data.OfType<RealMediaItemM>().Select(x => x.FilePath).Where(File.Exists).ToList(), true, false);
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
      VM.MainWindow.ToolsTabs.PersonDetailTab?.UpdateDisplayKeywordsIfContains(items);
      VM.MainWindow.ToolsTabs.PeopleTab?.Update(items);
      PeopleM.PeopleView?.Update(items);
      SegmentsView?.CvPeople.Update(items);
    };

    Db.People.ItemDeletedEvent += (_, e) => {
      Db.MediaItems.RemovePerson(e.Data);
      Db.Segments.RemovePerson(e.Data);
      PeopleM.Selected.Set(e.Data, false);
      PeopleM.PeopleView?.Remove(e.Data);
      VM.MainWindow.ToolsTabs.PeopleTab?.Remove(e.Data);
      SegmentsView?.CvPeople.Remove(e.Data);

      if (ReferenceEquals(VM.MainWindow.ToolsTabs.PersonDetailTab?.PersonM, e.Data))
        VM.MainWindow.ToolsTabs.Close(VM.MainWindow.ToolsTabs.PersonDetailTab);
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
      VM.MainWindow.ToolsTabs.PersonDetailTab?.Update(e.Data.Item2);
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
      VM.MainWindow.ToolsTabs.PersonDetailTab?.Update(items, true, false);
      SegmentsView?.CvSegments.Update(items);
    };

    Db.Segments.ItemDeletedEvent += (_, e) => {
      Db.People.OnSegmentPersonChanged(e.Data, e.Data.Person, null);
      SegmentsM.Selected.Set(e.Data, false);
    };

    Db.Segments.ItemsDeletedEvent += (_, e) => {
      Db.MediaItems.RemoveSegments(e.Data);
      VM.MainWindow.ToolsTabs.PersonDetailTab?.Update(e.Data.ToArray(), true, true);
      SegmentsView?.CvSegments.Remove(e.Data.ToArray());
      SegmentsM.SegmentsDrawerM.RemoveIfContains(e.Data.ToArray());
    };
  }

  private static void OpenSegmentsView() {
    var result = SegmentsView.GetSegmentsToLoadUserInput();
    if (result < 1) return;
    var segments = SegmentsView.GetSegments(result).ToArray();
    SegmentsView ??= new(SegmentsM);
    MainTabs.Activate(Res.IconSegment, "Segments", SegmentsView);
    if (MediaViewerM.IsVisible) VM.MainWindow.IsInViewMode = false;
    SegmentsView.Reload(segments);
  }
}