using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain {
  public sealed class Core {
    private static Core _instance;
    private static readonly object _lock = new();
    public static Core Instance { get { lock (_lock) { return _instance ??= new(); } } }

    public SimpleDB Sdb { get; }
    public MainTabsM MainTabsM { get; } = new();
    public ToolsTabsM ToolsTabsM { get; } = new();
    public TitleProgressBarM TitleProgressBarM { get; } = new();
    public ImageComparerM ImageComparerM { get; } = new();

    public CategoryGroupsM CategoryGroupsM { get; }
    public FavoriteFoldersM FavoriteFoldersM { get; }
    public FolderKeywordsM FolderKeywordsM { get; }
    public FoldersM FoldersM { get; }
    public GeoNamesM GeoNamesM { get; }
    public KeywordsM KeywordsM { get; }
    public MainWindowM MainWindowM { get; }
    public MediaItemsM MediaItemsM { get; }
    public MediaItemsViews MediaItemsViews { get; }
    public MediaViewerM MediaViewerM { get; }
    public PeopleM PeopleM { get; }
    public RatingsTreeM RatingsTreeM { get; }
    public SegmentsM SegmentsM { get; }
    public StatusPanelM StatusPanelM { get; }
    public TreeViewCategoriesM TreeViewCategoriesM { get; }
    public VideoClipsM VideoClipsM { get; }
    public ViewersM ViewersM { get; }
    public ViewerDetailM ViewerDetailM { get; }

    public delegate Dictionary<string, string> FileOperationDeleteFunc(List<string> items, bool recycle, bool silent);
    public static FileOperationDeleteFunc FileOperationDelete { get; set; }
    public static Func<Dialog, int> DialogHostShow { get; set; }
    public static Func<double> GetDisplayScale { get; set; }
    public static Settings Settings { get; set; }

    public RelayCommand<object> LoadedCommand { get; }

    private Core() {
      Tasks.SetUiTaskScheduler();
      Settings = new();
      Settings.Load();
      Sdb = new();

      LoadedCommand = new(Loaded);

      ViewersM = new(this); // CategoryGroupsM
      ViewerDetailM = new(ViewersM);
      SegmentsM = new(this); // MainTabsM, MediaViewerM, MainWindowM, MediaItemsViews
      CategoryGroupsM = new();
      FavoriteFoldersM = new();
      FolderKeywordsM = new();
      FoldersM = new(this, ViewersM); // FolderKeywordsM, MediaItemsM
      GeoNamesM = new();
      KeywordsM = new(CategoryGroupsM);
      MainWindowM = new(this);
      MediaItemsM = new(this, SegmentsM); // MediaItemsViews
      MediaItemsViews = new(this);
      MediaViewerM = new(this);
      PeopleM = new(this, CategoryGroupsM); // MainWindowM
      RatingsTreeM = new();
      StatusPanelM = new(this);
      TreeViewCategoriesM = new(this);
      VideoClipsM = new(MediaItemsM, MediaViewerM.MediaPlayerM);

      CategoryGroupsM.Categories.Add(Category.People, PeopleM);
      CategoryGroupsM.Categories.Add(Category.Keywords, KeywordsM);

      CategoryGroupsM.DataAdapter = new(CategoryGroupsM, KeywordsM, PeopleM);
      FavoriteFoldersM.DataAdapter = new(FavoriteFoldersM, FoldersM);
      FolderKeywordsM.DataAdapter = new(FoldersM);
      FoldersM.DataAdapter = new(FoldersM);
      GeoNamesM.DataAdapter = new(GeoNamesM);
      KeywordsM.DataAdapter = new(KeywordsM);
      MediaItemsM.DataAdapter = new(FoldersM, PeopleM, KeywordsM, GeoNamesM);
      PeopleM.DataAdapter = new(PeopleM, SegmentsM, KeywordsM);
      SegmentsM.DataAdapter = new(SegmentsM, MediaItemsM, PeopleM, KeywordsM);
      VideoClipsM.DataAdapter = new(VideoClipsM, MediaItemsM, KeywordsM, PeopleM);
      VideoClipsM.TreeCategory.GroupsM.DataAdapter = new(VideoClipsM.TreeCategory.GroupsM, VideoClipsM, MediaItemsM);
      ViewersM.DataAdapter = new(ViewersM, FoldersM, KeywordsM, CategoryGroupsM);
    }

    public Task InitAsync(IProgress<string> progress) {
      return Task.Run(() => {
        Sdb.AddDataAdapter(CategoryGroupsM.DataAdapter); // needs to be before People and Keywords
        Sdb.AddDataAdapter(KeywordsM.DataAdapter);
        Sdb.AddDataAdapter(FoldersM.DataAdapter); // needs to be before Viewers, FavoriteFolders and FolderKeywords
        Sdb.AddDataAdapter(FolderKeywordsM.DataAdapter); // needs to be before Viewers
        Sdb.AddDataAdapter(ViewersM.DataAdapter);
        Sdb.AddDataAdapter(PeopleM.DataAdapter); // needs to be before Segments
        Sdb.AddDataAdapter(GeoNamesM.DataAdapter);
        Sdb.AddDataAdapter(MediaItemsM.DataAdapter);
        Sdb.AddDataAdapter(VideoClipsM.TreeCategory.GroupsM.DataAdapter); // needs to be before VideoClips
        Sdb.AddDataAdapter(VideoClipsM.DataAdapter);
        Sdb.AddDataAdapter(FavoriteFoldersM.DataAdapter);
        Sdb.AddDataAdapter(SegmentsM.DataAdapter);

        SimpleDB.Migrate(4, DatabaseMigration.Resolver);

        Sdb.LoadAllTables(progress);
        Sdb.LinkReferences(progress);
        Sdb.ClearDataAdapters();

        AttachEvents();

        progress.Report("Loading drives");
      });
    }

    public void AfterInit() {
      if (KeywordsM.AutoAddedGroup == null)
        KeywordsM.AutoAddedGroup = CategoryGroupsM.GroupCreate("Auto Added", Category.Keywords);

      FoldersM.AddDrives();
      FolderKeywordsM.Load();
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

      FoldersM.FolderDeletedEventHandler += (_, e) => {
        FavoriteFoldersM.ItemDelete(e.Data);
        MediaItemsM.Delete(e.Data.MediaItems.ToList());
      };

      #region PeopleM EventHandlers

      PeopleM.AfterItemRenameEventHandler += (_, e) => {
        MediaItemsM.UpdateInfoBoxWithPerson((PersonM)e.Data);
      };

      PeopleM.PersonDeletedEventHandler += (_, e) => {
        MediaItemsM.RemovePersonFromMediaItems(e.Data);
        SegmentsM.RemovePersonFromSegments(e.Data);

        if (ReferenceEquals(PeopleM.PersonDetail?.PersonM, e.Data))
          ToolsTabsM.Deactivate(PeopleM.PersonDetail);
      };

      PeopleM.PeopleDeletedEvent += (_, e) => {
        PeopleM.PeopleView?.ReGroupItems(e.Data, true);
        SegmentsM.SegmentsView?.CvPeople.ReGroupItems(e.Data, true);
      };

      PeopleM.PeopleKeywordChangedEvent += (_, e) => {
        PeopleM.PeopleView?.ReGroupItems(e.Data, false);
        SegmentsM.SegmentsView?.CvPeople.ReGroupItems(e.Data, false);
      };

      #endregion

      #region KeywordsM EventHandlers

      KeywordsM.AfterItemRenameEventHandler += (_, e) => {
        MediaItemsM.UpdateInfoBoxWithKeyword((KeywordM)e.Data);
      };

      KeywordsM.KeywordDeletedEventHandler += (_, e) => {
        PeopleM.RemoveKeywordFromPeople(e.Data);
        SegmentsM.RemoveKeywordFromSegments(e.Data);
        MediaItemsM.RemoveKeywordFromMediaItems(e.Data);
      };

      #endregion

      #region MediaItemsM EventHandlers

      MediaItemsM.MediaItemDeletedEventHandler += (_, e) => {
        SegmentsM.Delete(e.Data.Segments);
      };

      MediaItemsM.MediaItemsDeletedEventHandler += (_, e) => {
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
            StatusPanelM.OnPropertyChanged(nameof(StatusPanelM.FilePath));
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

      SegmentsM.SegmentPersonChangeEventHandler += (_, e) => {
        PeopleM.SegmentPersonChange(e.Data.Item1, e.Data.Item2, e.Data.Item3);
      };

      SegmentsM.SegmentsPersonChangedEvent += (_, e) => {
        PeopleM.PersonDetail?.ReGroupIfContains(e.Data.Item1, false);
        SegmentsM.SegmentsView?.CvSegments.ReGroupItems(e.Data.Item1, false);
        SegmentsM.SegmentsView?.CvPeople.ReGroupItems(e.Data.Item2?.Where(x => x.Segment != null).ToArray(), false);

        // TODO fix this after changing logic that every segment has person until is changed to real person
        PeopleM.PeopleView?.ReGroupItems(e.Data.Item2?.Where(x => x.Segment != null).ToArray(), false);

        if (MediaViewerM.IsVisible)
          MediaViewerM.Current?.SetInfoBox();
      };

      SegmentsM.SegmentsKeywordChangedEvent += (_, e) => {
        PeopleM.PersonDetail?.ReGroupIfContains(e.Data.Item1, false);
        SegmentsM.SegmentsView?.CvSegments.ReGroupItems(e.Data.Item1, false);
      };

      SegmentsM.SegmentDeletedEventHandler += (_, e) => {
        PeopleM.PersonDetail?.ReGroupIfContains(new[] { e.Data }, true);
        SegmentsM.SegmentsDrawerM.Remove(e.Data);
        SegmentsM.SegmentsView?.CvSegments.ReGroupItems(new[] { e.Data }, true);
      };

      #endregion

      #region MainTabsM EventHandlers

      MainTabsM.TabClosedEventHandler += (_, e) => {
        switch (e.Data.Content) {
          case MediaItemsView miView:
            MediaItemsViews.CloseView(miView);
            break;
          case PeopleM people:
            people.Selected.DeselectAll();
            break;
        }
      };

      MainTabsM.PropertyChanged += (_, e) => {
        if (nameof(MainTabsM.Selected).Equals(e.PropertyName)) {
          MediaItemsViews.SetCurrentView(MainTabsM.Selected?.Content as MediaItemsView);

          if (MainTabsM.Selected?.Content as PeopleM == null)
            PeopleM.Selected.DeselectAll();
        }
      };

      #endregion
    }

    private void Loaded() {
      var scale = GetDisplayScale();
      MediaItemsViews.DefaultThumbScale = 1 / scale;
      SegmentsM.SetSegmentUiSize(SegmentsM.SegmentSize / scale, 14);
      MediaItemsM.OnPropertyChanged(nameof(MediaItemsM.MediaItemsCount));
    }
  }
}
