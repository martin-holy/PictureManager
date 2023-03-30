using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MH.Utils.Dialogs;
using MH.Utils.Interfaces;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain {
  public sealed class Core : ILogger {
    public string CachePath { get; set; }
    public int ThumbnailSize { get; set; }
    public ObservableCollection<LogItem> Log { get; } = new();
    public SimpleDB.SimpleDB Sdb { get; }
    public MainTabsM MainTabsM { get; } = new();
    public ToolsTabsM ToolsTabsM { get; } = new();
    public TitleProgressBarM TitleProgressBarM { get; } = new();

    public CategoryGroupsM CategoryGroupsM { get; }
    public FavoriteFoldersM FavoriteFoldersM { get; }
    public FolderKeywordsM FolderKeywordsM { get; }
    public FoldersM FoldersM { get; }
    public GeoNamesM GeoNamesM { get; }
    public KeywordsM KeywordsM { get; }
    public MainWindowM MainWindowM { get; }
    public MediaItemsM MediaItemsM { get; }
    public MediaItemSizesTreeM MediaItemSizesTreeM { get; }
    public MediaViewerM MediaViewerM { get; }
    public PeopleM PeopleM { get; }
    public RatingsTreeM RatingsTreeM { get; }
    public SegmentsM SegmentsM { get; }
    public StatusPanelM StatusPanelM { get; }
    public ThumbnailsGridsM ThumbnailsGridsM { get; }
    public TreeViewCategoriesM TreeViewCategoriesM { get; }
    public VideoClipsM VideoClipsM { get; }
    public ViewersM ViewersM { get; }

    public static Func<IDialog, int> DialogHostShow { get; set; }

    private static TaskScheduler UiTaskScheduler { get; set; }

    private Core() {
      UiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

      Sdb = new(this);

      ViewersM = new(this); // CategoryGroupsM
      SegmentsM = new(this); // MainTabsM, MediaViewerM, MainWindowM, ThumbnailsGridsM
      CategoryGroupsM = new();
      FavoriteFoldersM = new();
      FolderKeywordsM = new();
      FoldersM = new(this, ViewersM); // FolderKeywordsM, MediaItemsM
      GeoNamesM = new();
      KeywordsM = new(CategoryGroupsM);
      MainWindowM = new(this);
      MediaItemsM = new(this, SegmentsM, ViewersM); // ThumbnailsGridsM
      MediaItemSizesTreeM = new();
      MediaViewerM = new();
      PeopleM = new(CategoryGroupsM);
      RatingsTreeM = new();
      StatusPanelM = new(this);
      ThumbnailsGridsM = new(this);
      TreeViewCategoriesM = new(this);
      VideoClipsM = new(MediaItemsM, MediaViewerM.MediaPlayerM);

      CategoryGroupsM.Categories.Add(Category.People, PeopleM);
      CategoryGroupsM.Categories.Add(Category.Keywords, KeywordsM);

      CategoryGroupsM.DataAdapter = new(CategoryGroupsM, KeywordsM, PeopleM);
      FavoriteFoldersM.DataAdapter = new(FavoriteFoldersM, FoldersM);
      FoldersM.DataAdapter = new(FoldersM);
      GeoNamesM.DataAdapter = new(GeoNamesM);
      KeywordsM.DataAdapter = new(KeywordsM, CategoryGroupsM);
      MediaItemsM.DataAdapter = new(FoldersM, PeopleM, KeywordsM, GeoNamesM);
      PeopleM.DataAdapter = new(PeopleM, SegmentsM, KeywordsM);
      SegmentsM.DataAdapter = new(SegmentsM, MediaItemsM, PeopleM, KeywordsM);
      VideoClipsM.DataAdapter = new(VideoClipsM, MediaItemsM, KeywordsM, PeopleM);
      VideoClipsM.GroupsM.DataAdapter = new(VideoClipsM.GroupsM, VideoClipsM, MediaItemsM);
      ViewersM.DataAdapter = new(ViewersM, FoldersM, KeywordsM, FolderKeywordsM, CategoryGroupsM);
    }

    public Task InitAsync(IProgress<string> progress) {
      return Task.Run(() => {
        Sdb.AddDataAdapter(CategoryGroupsM.DataAdapter); // needs to be before People and Keywords
        Sdb.AddDataAdapter(KeywordsM.DataAdapter);
        Sdb.AddDataAdapter(FoldersM.DataAdapter); // needs to be before Viewers and FavoriteFolders
        Sdb.AddDataAdapter(ViewersM.DataAdapter);
        Sdb.AddDataAdapter(PeopleM.DataAdapter); // needs to be before Segments
        Sdb.AddDataAdapter(GeoNamesM.DataAdapter);
        Sdb.AddDataAdapter(MediaItemsM.DataAdapter);
        Sdb.AddDataAdapter(VideoClipsM.GroupsM.DataAdapter); // needs to be before VideoClips
        Sdb.AddDataAdapter(VideoClipsM.DataAdapter);
        Sdb.AddDataAdapter(FavoriteFoldersM.DataAdapter);
        Sdb.AddDataAdapter(SegmentsM.DataAdapter);

        SimpleDB.SimpleDB.Migrate(2, DatabaseMigration.Resolver, this);

        Sdb.LoadAllTables(progress);
        Sdb.LinkReferences(progress);
        Sdb.ClearDataAdapters();

        AttachEvents();

        progress.Report("Loading drives");
      });
    }

    private void AttachEvents() {
      MainWindowM.PropertyChanged += (_, e) => {
        if (nameof(MainWindowM.IsFullScreen).Equals(e.PropertyName)) {
          var isFullScreen = MainWindowM.IsFullScreen;

          MediaViewerM.IsVisible = isFullScreen;

          if (!isFullScreen) {
            ThumbnailsGridsM.Current?.SelectAndScrollToCurrentMediaItem();
            TreeViewCategoriesM.MarkUsedKeywordsAndPeople();

            if (SegmentsM.NeedReload) {
              SegmentsM.NeedReload = false;
              SegmentsM.Reload();
            }

            MediaViewerM.Deactivate();
            StatusPanelM.CurrentMediaItemM = ThumbnailsGridsM.Current?.CurrentMediaItem;
          }
        }
      };

      FoldersM.FolderDeletedEventHandler += (_, e) => {
        FavoriteFoldersM.ItemDelete(e.Data);
        MediaItemsM.Delete(e.Data.MediaItems.ToArray());
      };

      PeopleM.AfterItemRenameEventHandler += (_, e) => {
        MediaItemsM.UpdateInfoBoxWithPerson((PersonM)e.Data);
      };

      PeopleM.PersonDeletedEventHandler += (_, e) => {
        MediaItemsM.RemovePersonFromMediaItems(e.Data);
        SegmentsM.RemovePersonFromSegments(e.Data);
      };

      PeopleM.PersonTopSegmentsChangedEventHandler += (_, e) => {
        if (SegmentsM.MatchingAutoSort && SegmentsM.Loaded.Any(x => e.Data.Equals(x.Person)))
          SegmentsM.Reload(false, true);
      };

      PeopleM.PeopleKeywordChangedEvent += delegate {
        SegmentsM.Reload();
      };

      KeywordsM.AfterItemRenameEventHandler += (_, e) => {
        MediaItemsM.UpdateInfoBoxWithKeyword((KeywordM)e.Data);
      };

      KeywordsM.KeywordDeletedEventHandler += (_, e) => {
        PeopleM.RemoveKeywordFromPeople(e.Data);
        SegmentsM.RemoveKeywordFromSegments(e.Data);
        MediaItemsM.RemoveKeywordFromMediaItems(e.Data);
      };

      MediaItemsM.MediaItemDeletedEventHandler += (_, e) => {
        SegmentsM.Delete(e.Data.Segments);
        ThumbnailsGridsM.RemoveMediaItem(e.Data);
      };

      MediaItemsM.MediaItemsDeletedEventHandler += async (_, e) => {
        if (ThumbnailsGridsM.Current?.NeedReload == true)
          await ThumbnailsGridsM.Current.ThumbsGridReloadItems();

        if (MediaViewerM.IsVisible) {
          MediaViewerM.MediaItems.Remove(e.Data[0]);
          if (MediaItemsM.Current != null)
            MediaViewerM.OnPropertyChanged(nameof(MediaViewerM.Current));
          else
            MainWindowM.IsFullScreen = false;
        }
      };

      MediaItemsM.MediaItemsOrientationChangedEventHandler += async (_, e) => {
        if (MediaViewerM.IsVisible && e.Data.Contains(MediaItemsM.Current))
          MediaViewerM.OnPropertyChanged(nameof(MediaViewerM.Current));

        foreach (var __ in e.Data)
          MediaItemsM.DataAdapter.IsModified = true;

        await ThumbnailsGridsM.ReloadGridsIfContains(e.Data);
      };

      MediaItemsM.MetadataChangedEventHandler += (_, _) => {
        TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
        StatusPanelM.UpdateRating();
      };

      MediaItemsM.PropertyChanged += (_, e) => {
        if (nameof(MediaItemsM.Current).Equals(e.PropertyName)) {
          MediaViewerM.SetCurrent(MediaItemsM.Current);

          if (MainWindowM.IsFullScreen)
            TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
        }
      };

      SegmentsM.SegmentPersonChangeEventHandler += (_, e) => {
        PeopleM.SegmentPersonChange(e.Data.Item1, e.Data.Item2, e.Data.Item3);
        e.Data.Item1.MediaItem.SetInfoBox();
      };

      ThumbnailsGridsM.PropertyChanged += (_, e) => {
        if (nameof(ThumbnailsGridsM.Current).Equals(e.PropertyName)) {
          MediaItemSizesTreeM.Size.CurrentGrid = ThumbnailsGridsM.Current;
          TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
          MainWindowM.OnPropertyChanged(nameof(MainWindowM.CanOpenStatusPanel));
        }
      };

      MediaViewerM.PropertyChanged += (_, e) => {
        switch (e.PropertyName) {
          case nameof(MediaViewerM.IsVisible):
            StatusPanelM.OnPropertyChanged(nameof(StatusPanelM.FilePath));
            MainWindowM.OnPropertyChanged(nameof(MainWindowM.CanOpenStatusPanel));
            break;
          case nameof(MediaViewerM.Current):
            SegmentsM.SegmentsRectsM.MediaItem = MediaViewerM.Current;

            if (MediaItemsM.Current != MediaViewerM.Current)
              MediaItemsM.Current = MediaViewerM.Current;
            break;
        }
      };
    }

    private MessageDialog ToggleOrGetDialog(string title, object item, string itemName) {
      var sCount = SegmentsM.Selected.Count;
      var pCount = item is PersonM ? 0 : PeopleM.Selected.Count;
      var miCount = MediaItemsM.IsEditModeOn ? ThumbnailsGridsM.Current?.SelectedCount ?? 0 : 0;
      if (sCount == 0 && pCount == 0 && miCount == 0) return null;

      var msgA = $"Do you want to toggle #{itemName} on selected";
      var msgB = new List<string>();
      var msgS = sCount > 1 ? $"Segments ({sCount})" : "Segment";
      var msgP = pCount > 1 ? $"People ({pCount})" : "Person";
      var msgMi = miCount > 1 ? $"Media Items ({miCount})" : "Media Item";
      var oneOption = new[] { sCount, pCount, miCount }.Count(x => x > 0) == 1;
      var buttons = new List<DialogButton>();

      void AddOption(string msg, int result, string icon) {
        buttons.Add(oneOption
          ? new("YES", result, Res.IconCheckMark, true)
          : new(msg, result, icon));
        msgB.Add(msg);
      }

      if (sCount > 0) AddOption(msgS, 1, Res.IconEquals);
      if (pCount > 0) AddOption(msgP, 2, Res.IconPeople);
      if (miCount > 0) AddOption(msgMi, 3, Res.IconImage);
      if (oneOption) buttons.Add(new("NO", 0, Res.IconXCross, false, true));

      if (oneOption && miCount > 0) {
        MediaItemsM.SetMetadata(item);
        return null;
      }

      var msg = oneOption
        ? $"{msgA} {msgB[0]}?"
        : $"{msgA} {string.Join(" or ", msgB)}?";

      return new(title, msg, Res.IconQuestion, true, buttons.ToArray());
    }

    public void ToggleKeyword(KeywordM keyword) {
      if (ToggleOrGetDialog("Toggle Keyword", keyword, keyword.FullName) is not { } md) return;

      switch (DialogHostShow(md)) {
        case 1: SegmentsM.ToggleKeywordOnSelected(keyword); break;
        case 2: PeopleM.ToggleKeywordOnSelected(keyword); break;
        case 3: MediaItemsM.SetMetadata(keyword); break;
      }
    }

    public void TogglePerson(PersonM person) {
      if (ToggleOrGetDialog("Toggle Person", person, person.Name) is not { } md) return;

      switch (DialogHostShow(md)) {
        case 1: SegmentsM.SetSelectedAsPerson(person); break;
        case 3: MediaItemsM.SetMetadata(person); break;
      }
    }

    public void LogError(Exception ex) =>
      LogError(ex, string.Empty);

    public void LogError(Exception ex, string msg) =>
      RunOnUiThread(() =>
        Log.Add(new(
          string.IsNullOrEmpty(msg)
            ? ex.Message
            : msg,
          $"{msg}\n{ex.Message}\n{ex.StackTrace}")));

    private static Core _instance;
    private static readonly object Lock = new();
    public static Core Instance {
      get {
        lock (Lock) {
          return _instance ??= new();
        }
      }
    }

    public static Task RunOnUiThread(Action action) {
      var task = new Task(action);
      task.Start(UiTaskScheduler);
      return task;
    }

    public static Task<T> RunOnUiThread<T>(Func<T> func) {
      var task = new Task<T>(func);
      task.Start(UiTaskScheduler);
      return task;
    }
  }
}
