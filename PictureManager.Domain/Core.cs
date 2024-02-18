using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.Repositories;
using PictureManager.Domain.Services;
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

  public static CoreR R { get; } = new();
  public static CoreS S { get; private set; }
  public static CoreVM VM { get; private set; }
  public static Settings Settings { get; } = new();

  public static RatingsTreeCategory RatingsTreeCategory { get; } = new();

  public static TabControl MainTabs { get; } = new() { CanCloseTabs = true };
  public static MediaItemsViews MediaItemsViews { get; } = new();
  public TitleProgressBarM TitleProgressBarM { get; } = new();
  public static TreeViewCategoriesM TreeViewCategoriesM { get; } = new();
  public static VideoDetail VideoDetail { get; } = new();
  public static IPlatformSpecificUiMediaPlayer UiFullVideo { get; set; }
  public static IPlatformSpecificUiMediaPlayer UiDetailVideo { get; set; }
  public static IVideoFrameSaver VideoFrameSaver { get; set; }
  public static SegmentsView SegmentsView { get; set; }

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
      R.AddDataAdapters();
      Drives.UpdateSerialNumbers();
      progress.Report("Migrating Database");
      SimpleDB.Migrate(7, DatabaseMigration.Resolver);
      R.LoadAllTables(progress);
      R.LinkReferences(progress);
      R.ClearDataAdapters();
      R.SetIsReady();
      progress.Report("Loading UI");
    });
  }

  public void AfterInit() {
    var scale = GetDisplayScale();
    MediaItemsViews.DefaultThumbScale = 1 / scale;
    SegmentS.SetSegmentUiSize(scale);
    S = new(R);
    VM = new(S, R);
    AttachEvents();
    AttachVMEvents();

    R.Keyword.Tree.AutoAddedGroup ??=
      R.CategoryGroup.ItemCreate(R.Keyword.Tree, "Auto Added");

    R.Folder.Tree.AddDrives();
    S.Viewer.SetCurrent(R.Viewer.All.SingleOrDefault(x => x.IsDefault));
    S.Viewer.Current?.UpdateHashSets();
    TreeViewCategoriesM.AddCategories();
    R.CategoryGroup.AddCategory(R.Person.Tree);
    R.CategoryGroup.AddCategory(R.Keyword.Tree);
    VideoDetail.MediaPlayer.SetView(UiFullVideo);
    VideoDetail.MediaPlayer.SetView(UiDetailVideo);
    OpenSegmentsViewCommand = new(OpenSegmentsView, Res.IconSegment, "Segments View");
  }

  private void AttachVMEvents() {
    AttachVMMediaItemsEventHandlers();

    VM.MainWindow.PropertyChanged += (_, e) => {
      if (nameof(VM.MainWindow.IsInViewMode).Equals(e.PropertyName)) {
        var isInViewMode = VM.MainWindow.IsInViewMode;

        VM.MediaViewer.IsVisible = isInViewMode;

        if (isInViewMode) {
          VideoDetail.MediaPlayer.SetView(UiFullVideo);
          S.Segment.Rect.MediaItem = VM.MediaItem.Current;
        }
        else {
          MediaItemsViews.SelectAndScrollToCurrentMediaItem();
          VM.MediaViewer.Deactivate();
          VideoDetail.MediaPlayer.SetView(UiDetailVideo);
        }

        TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      }
    };
  }

  private void AttachVMMediaItemsEventHandlers() {
    VM.MediaItem.PropertyChanged += (_, e) => {
      if (nameof(VM.MediaItem.Current).Equals(e.PropertyName)) {
        VM.MainWindow.StatusBar.Update();
        VideoDetail.SetCurrent(VM.MediaItem.Current);

        if (VM.MainWindow.IsInViewMode) {
          TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
          S.Segment.Rect.MediaItem = VM.MediaItem.Current;
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
        R.GeoName.ApiLimitExceeded = false;
    };

    VideoDetail.MediaPlayer.RepeatEndedEvent += delegate {
      VM.MediaViewer.OnPlayerRepeatEnded();
    };

    VideoDetail.CurrentVideoItems.Selected.ItemsChangedEventHandler += (_, e) => {
      var vi = e.Data.FirstOrDefault();
      VM.MediaItem.Current = (MediaItemM)vi ?? VideoDetail.Current;
      VideoDetail.MediaPlayer.SetCurrent(vi);
    };

    VM.MediaViewer.PropertyChanged += (_, e) => {
      switch (e.PropertyName) {
        case nameof(VM.MediaViewer.IsVisible):
          VM.MainWindow.StatusBar.Update();
          VM.MainWindow.StatusBar.OnPropertyChanged(nameof(VM.MainWindow.StatusBar.IsCountVisible));
          break;
        case nameof(VM.MediaViewer.Current):
          if (VM.MediaViewer.Current != null && !ReferenceEquals(VM.MediaItem.Current, VM.MediaViewer.Current))
            VM.MediaItem.Current = VM.MediaViewer.Current;
          else
            VideoDetail.SetCurrent(VM.MediaViewer.Current, true);
          break;
        case nameof(VM.MediaViewer.Scale):
          S.Segment.Rect.UpdateScale(VM.MediaViewer.Scale);
          break;
      }
    };

    MainTabs.TabClosedEvent += tab => {
      switch (tab.Data) {
        case MediaItemsView miView:
          MediaItemsViews.CloseView(miView);
          break;
        case PersonS people:
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
    R.CategoryGroup.ItemDeletedEvent += (_, e) => {
      R.Keyword.MoveGroupItemsToRoot(e.Data);
      R.Person.MoveGroupItemsToRoot(e.Data);
    };

  private static void AttachFoldersEventHandlers() {
    R.Folder.ItemCreatedEvent += (_, e) =>
      R.FolderKeyword.LoadIfContains((FolderM)e.Data.Parent);

    R.Folder.ItemRenamedEvent += (_, e) => {
      R.FolderKeyword.LoadIfContains(e.Data);
      VM.MainWindow.StatusBar.UpdateFilePath();
    };

    R.Folder.ItemDeletedEvent += (_, e) => {
      R.FavoriteFolder.ItemDeleteByFolder(e.Data);
      R.MediaItem.ItemsDelete(e.Data.MediaItems.Cast<MediaItemM>().ToArray());
      FolderS.DeleteFromDisk(e.Data);
    };

    R.Folder.ItemsDeletedEvent += (_, _) =>
      R.FolderKeyword.Reload();

    S.Folder.ItemCopiedEvent += (_, _) =>
      R.FolderKeyword.Reload();

    S.Folder.ItemMovedEvent += (_, _) => {
      R.FolderKeyword.Reload();
      VM.MainWindow.StatusBar.UpdateFilePath();
    };
  }

  private static void AttachGeoLocationsEventHandlers() {
    R.GeoLocation.ItemUpdatedEvent += (_, e) =>
      R.MediaItem.ModifyIfContains(e.Data);

    R.GeoLocation.ItemDeletedEvent += (_, e) =>
      R.MediaItem.ModifyIfContains(e.Data);
  }

  private static void AttachGeoNamesEventHandlers() {
    R.GeoName.ItemDeletedEvent += (_, e) =>
      R.GeoLocation.RemoveGeoName(e.Data);
  }

  private static void AttachKeywordsEventHandlers() {
    R.Keyword.ItemRenamedEvent += (_, e) =>
      R.MediaItem.ModifyIfContains(e.Data);

    R.Keyword.ItemDeletedEvent += (_, e) => {
      R.Person.RemoveKeyword(e.Data);
      R.Segment.RemoveKeyword(e.Data);
      R.MediaItem.RemoveKeyword(e.Data);
    };
  }

  private static void AttachMediaItemsEventHandlers() {
    R.MediaItem.ItemCreatedEvent += (_, _) =>
      VM.UpdateMediaItemsCount();

    R.MediaItem.ItemRenamedEvent += (_, _) => {
      VM.MediaItem.OnPropertyChanged(nameof(VM.MediaItem.Current));
      MediaItemsViews.Current?.SoftLoad(MediaItemsViews.Current.FilteredItems, true, false);
    };

    R.MediaItem.MetadataChangedEvent += items => {
      var all = items.OfType<VideoItemM>().Select(x => x.Video).Concat(items).Distinct().ToArray();
      VM.MediaItem.OnMetadataChanged(all);
      MediaItemsViews.UpdateViews(all);
      VideoDetail.CurrentVideoItems.Update(items.OfType<VideoItemM>().ToArray());
      TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      VM.MainWindow.StatusBar.UpdateRating();
      VM.UpdateModifiedMediaItemsCount();
    };

    R.MediaItem.OrientationChangedEvent += items => {
      foreach (var rmi in items) {
        rmi.SetThumbSize(true);
        File.Delete(rmi.FilePathCache);
      }

      if (VM.MediaViewer.IsVisible && items.Contains(VM.MediaViewer.Current))
        VM.MediaViewer.Current = VM.MediaViewer.Current;

      MediaItemsViews.ReWrapViews(items.Cast<MediaItemM>().ToArray());
      if (items.Contains(VideoDetail.Current))
        VideoDetail.CurrentVideoItems.ReWrapAll();
    };

    R.MediaItem.ItemDeletedEvent += (_, e) => {
      R.Segment.ItemsDelete(e.Data.Segments?.ToArray());
      if (e.Data.GeoLocation != null)
        R.MediaItemGeoLocation.IsModified = true;
    };

    R.MediaItem.ItemsDeletedEvent += (_, e) => {
      VM.MediaItem.Current = VM.MediaViewer.IsVisible && e.Data.All(x => x is RealMediaItemM)
        ? VM.MediaViewer.MediaItems.GetNextOrPreviousItem(e.Data)
        : e.Data.OfType<VideoItemM>().FirstOrDefault()?.Video;

      VM.UpdateMediaItemsCount();
      VM.UpdateModifiedMediaItemsCount();
      MediaItemsViews.RemoveMediaItems(e.Data);
      VideoDetail.CurrentVideoItems.Remove(e.Data.OfType<VideoItemM>().ToArray());

      if (VM.MediaViewer.IsVisible) {
        VM.MediaViewer.MediaItems.Remove(e.Data[0]);
        if (VM.MediaItem.Current != null)
          VM.MediaViewer.Current = VM.MediaItem.Current;
        else
          VM.MainWindow.IsInViewMode = false;
      }
      
      FileOperationDelete(e.Data.OfType<RealMediaItemM>().Select(x => x.FilePath).Where(File.Exists).ToList(), true, false);
    };

    MediaItemsViews.PropertyChanged += (_, e) => {
      if (nameof(MediaItemsViews.Current).Equals(e.PropertyName)) {
        TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
        VM.MainWindow.StatusBar.OnPropertyChanged(nameof(VM.MainWindow.StatusBar.IsCountVisible));
      }
    };
  }

  private static void AttachPeopleEventHandlers() {
    R.Person.ItemRenamedEvent += (_, e) =>
      R.MediaItem.ModifyIfContains(e.Data);

    R.Person.KeywordsChangedEvent += items => {
      VM.MainWindow.ToolsTabs.PersonDetailTab?.UpdateDisplayKeywordsIfContains(items);
      VM.MainWindow.ToolsTabs.PeopleTab?.Update(items);
      S.Person.PeopleView?.Update(items);
      SegmentsView?.CvPeople.Update(items);
    };

    R.Person.ItemDeletedEvent += (_, e) => {
      R.MediaItem.RemovePerson(e.Data);
      R.Segment.RemovePerson(e.Data);
      S.Person.Selected.Set(e.Data, false);
      S.Person.PeopleView?.Remove(e.Data);
      VM.MainWindow.ToolsTabs.PeopleTab?.Remove(e.Data);
      SegmentsView?.CvPeople.Remove(e.Data);

      if (ReferenceEquals(VM.MainWindow.ToolsTabs.PersonDetailTab?.PersonM, e.Data))
        VM.MainWindow.ToolsTabs.Close(VM.MainWindow.ToolsTabs.PersonDetailTab);
    };
  }

  private static void AttachSegmentsEventHandlers() {
    R.Segment.ItemCreatedEvent += (_, e) => {
      R.MediaItem.AddSegment(e.Data);
      SegmentsView?.CvSegments.Update(e.Data, false);
    };

    R.Segment.SegmentPersonChangedEvent += (_, e) => {
      R.Person.OnSegmentPersonChanged(e.Data.Item1, e.Data.Item2, e.Data.Item3);
    };

    R.Segment.SegmentsPersonChangedEvent += (_, e) => {
      R.Person.OnSegmentsPersonChanged(e.Data.Item1, e.Data.Item2, e.Data.Item3);
      R.MediaItem.TogglePerson(e.Data.Item2);
      VM.MainWindow.ToolsTabs.PersonDetailTab?.Update(e.Data.Item2);
      S.Person.PeopleView?.Update(e.Data.Item3);
      S.Segment.Selected.DeselectAll();

      if (SegmentsView != null) {
        SegmentsView.CvSegments.Update(e.Data.Item2, false);
        var pIn = e.Data.Item2.GetPeople().ToArray();
        var pOut = e.Data.Item3.Except(pIn).ToArray();
        SegmentsView.CvPeople.Update(pIn, false);
        SegmentsView.CvPeople.Remove(pOut);
      }
    };

    R.Segment.KeywordsChangedEvent += items => {
      R.MediaItem.ModifyIfContains(items);
      VM.MainWindow.ToolsTabs.PersonDetailTab?.Update(items, true, false);
      SegmentsView?.CvSegments.Update(items);
    };

    R.Segment.ItemDeletedEvent += (_, e) => {
      R.Person.OnSegmentPersonChanged(e.Data, e.Data.Person, null);
      S.Segment.Selected.Set(e.Data, false);
    };

    R.Segment.ItemsDeletedEvent += (_, e) => {
      R.MediaItem.RemoveSegments(e.Data);
      VM.MainWindow.ToolsTabs.PersonDetailTab?.Update(e.Data.ToArray(), true, true);
      SegmentsView?.CvSegments.Remove(e.Data.ToArray());
      VM.SegmentsDrawer.RemoveIfContains(e.Data.ToArray());
    };
  }

  private static void OpenSegmentsView() {
    var result = SegmentsView.GetSegmentsToLoadUserInput();
    if (result < 1) return;
    var segments = SegmentsView.GetSegments(result).ToArray();
    SegmentsView ??= new(S.Segment);
    MainTabs.Activate(Res.IconSegment, "Segments", SegmentsView);
    if (VM.MediaViewer.IsVisible) VM.MainWindow.IsInViewMode = false;
    SegmentsView.Reload(segments);
  }
}