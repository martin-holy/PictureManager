using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.Repositories;
using PictureManager.Domain.Services;
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

  public delegate Dictionary<string, string> FileOperationDeleteFunc(List<string> items, bool recycle, bool silent);
  public static FileOperationDeleteFunc FileOperationDelete { get; set; }
  public static Func<double> GetDisplayScale { get; set; }

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
    SegmentS.SetSegmentUiSize(scale);
    S = new(R);
    VM = new(S, R);
    AttachEvents();

    R.Keyword.Tree.AutoAddedGroup ??=
      R.CategoryGroup.ItemCreate(R.Keyword.Tree, "Auto Added");

    R.Folder.Tree.AddDrives();
    S.Viewer.SetCurrent(R.Viewer.All.SingleOrDefault(x => x.IsDefault));
    S.Viewer.Current?.UpdateHashSets();
    VM.MainWindow.TreeViewCategories.AddCategories();
    R.CategoryGroup.AddCategory(R.Person.Tree);
    R.CategoryGroup.AddCategory(R.Keyword.Tree);
    VM.Video.MediaPlayer.SetView(CoreVM.UiFullVideo);
    VM.Video.MediaPlayer.SetView(CoreVM.UiDetailVideo);
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
      if (e.Is(nameof(Settings.GeoNamesUserName)))
        R.GeoName.ApiLimitExceeded = false;
    };

    VM.MainWindow.PropertyChanged += (_, e) => {
      if (e.Is(nameof(VM.MainWindow.IsInViewMode))) {
        var isInViewMode = VM.MainWindow.IsInViewMode;

        VM.MediaViewer.IsVisible = isInViewMode;

        if (isInViewMode) {
          VM.Video.MediaPlayer.SetView(CoreVM.UiFullVideo);
          S.Segment.Rect.MediaItem = VM.MediaItem.Current;
        }
        else {
          VM.MediaItem.Views.SelectAndScrollToCurrentMediaItem();
          VM.MediaViewer.Deactivate();
          VM.Video.MediaPlayer.SetView(CoreVM.UiDetailVideo);
        }

        VM.MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
      }
    };

    VM.Video.MediaPlayer.RepeatEndedEvent += delegate {
      VM.MediaViewer.OnPlayerRepeatEnded();
    };

    VM.Video.CurrentVideoItems.Selected.ItemsChangedEventHandler += (_, e) => {
      var vi = e.Data.FirstOrDefault();
      VM.MediaItem.Current = (MediaItemM)vi ?? VM.Video.Current;
      VM.Video.MediaPlayer.SetCurrent(vi);
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
            VM.Video.SetCurrent(VM.MediaViewer.Current, true);
          break;
        case nameof(VM.MediaViewer.Scale):
          S.Segment.Rect.UpdateScale(VM.MediaViewer.Scale);
          break;
      }
    };

    VM.MainTabs.TabClosedEvent += tab => {
      switch (tab.Data) {
        case MediaItemsViewVM miView:
          VM.MediaItem.Views.CloseView(miView);
          break;
        case PersonS people:
          people.Selected.DeselectAll();
          break;
        case Settings settings:
          settings.OnClosing();
          break;
      }
    };

    VM.MainTabs.PropertyChanged += (_, e) => {
      if (e.Is(nameof(VM.MainTabs.Selected)))
        VM.MediaItem.Views.SetCurrentView(VM.MainTabs.Selected?.Data as MediaItemsViewVM);
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
      VM.MediaItem.Views.Current?.SoftLoad(VM.MediaItem.Views.Current.FilteredItems, true, false);
    };

    R.MediaItem.MetadataChangedEvent += items => {
      var all = items.OfType<VideoItemM>().Select(x => x.Video).Concat(items).Distinct().ToArray();
      VM.MediaItem.OnMetadataChanged(all);
      VM.MediaItem.Views.UpdateViews(all);
      VM.Video.CurrentVideoItems.Update(items.OfType<VideoItemM>().ToArray());
      VM.MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
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

      VM.MediaItem.Views.ReWrapViews(items.Cast<MediaItemM>().ToArray());
      if (items.Contains(VM.Video.Current))
        VM.Video.CurrentVideoItems.ReWrapAll();
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
      VM.MediaItem.Views.RemoveMediaItems(e.Data);
      VM.Video.CurrentVideoItems.Remove(e.Data.OfType<VideoItemM>().ToArray());

      if (VM.MediaViewer.IsVisible) {
        if (VM.MediaItem.Current == null)
          VM.MainWindow.IsInViewMode = false;
        else
          VM.MediaViewer.Remove(e.Data[0], VM.MediaItem.Current);
      }
      
      FileOperationDelete(e.Data.OfType<RealMediaItemM>().Select(x => x.FilePath).Where(File.Exists).ToList(), true, false);
    };

    VM.MediaItem.PropertyChanged += (_, e) => {
      if (e.Is(nameof(VM.MediaItem.Current))) {
        VM.MainWindow.StatusBar.Update();
        VM.Video.SetCurrent(VM.MediaItem.Current);

        if (VM.MainWindow.IsInViewMode) {
          VM.MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
          S.Segment.Rect.MediaItem = VM.MediaItem.Current;
        }
      }
    };

    VM.MediaItem.Views.PropertyChanged += (_, e) => {
      if (e.Is(nameof(VM.MediaItem.Views.Current))) {
        VM.MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
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
      VM.People?.Update(items);
      VM.SegmentsMatching?.CvPeople.Update(items);
    };

    R.Person.ItemDeletedEvent += (_, e) => {
      R.MediaItem.RemovePerson(e.Data);
      R.Segment.RemovePerson(e.Data);
      S.Person.Selected.Set(e.Data, false);
      VM.People?.Remove(e.Data);
      VM.MainWindow.ToolsTabs.PeopleTab?.Remove(e.Data);
      VM.SegmentsMatching?.CvPeople.Remove(e.Data);

      if (ReferenceEquals(VM.MainWindow.ToolsTabs.PersonDetailTab?.PersonM, e.Data))
        VM.MainWindow.ToolsTabs.Close(VM.MainWindow.ToolsTabs.PersonDetailTab);
    };
  }

  private static void AttachSegmentsEventHandlers() {
    R.Segment.ItemCreatedEvent += (_, e) => {
      R.MediaItem.AddSegment(e.Data);
      VM.SegmentsMatching?.CvSegments.Update(e.Data, false);
    };

    R.Segment.SegmentPersonChangedEvent += (_, e) => {
      R.Person.OnSegmentPersonChanged(e.Data.Item1, e.Data.Item2, e.Data.Item3);
    };

    R.Segment.SegmentsPersonChangedEvent += (_, e) => {
      R.Person.OnSegmentsPersonChanged(e.Data.Item1, e.Data.Item2, e.Data.Item3);
      R.MediaItem.TogglePerson(e.Data.Item2);
      VM.MainWindow.ToolsTabs.PersonDetailTab?.Update(e.Data.Item2);
      VM.People?.Update(e.Data.Item3);
      S.Segment.Selected.DeselectAll();
      VM.SegmentsMatching?.OnSegmentsPersonChanged(e.Data.Item2);
    };

    R.Segment.KeywordsChangedEvent += items => {
      R.MediaItem.ModifyIfContains(items);
      VM.MainWindow.ToolsTabs.PersonDetailTab?.Update(items, true, false);
      VM.SegmentsMatching?.CvSegments.Update(items);
    };

    R.Segment.ItemDeletedEvent += (_, e) => {
      R.Person.OnSegmentPersonChanged(e.Data, e.Data.Person, null);
      S.Segment.Selected.Set(e.Data, false);
    };

    R.Segment.ItemsDeletedEvent += (_, e) => {
      R.MediaItem.RemoveSegments(e.Data);
      VM.MainWindow.ToolsTabs.PersonDetailTab?.Update(e.Data.ToArray(), true, true);
      VM.SegmentsMatching?.CvSegments.Remove(e.Data.ToArray());
      VM.SegmentsDrawer.RemoveIfContains(e.Data.ToArray());
    };
  }
}