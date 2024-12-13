using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.GeoLocation;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Rating;
using PictureManager.Common.Features.Segment;
using PictureManager.Common.Features.Viewer;
using PictureManager.Common.Layout;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace PictureManager.Common;

public sealed class CoreVM : ObservableObject {
  private readonly CoreS _coreS;
  private readonly CoreR _coreR;

  public ToggleDialog ToggleDialog { get; } = new();

  public GeoNameVM GeoName { get; }
  public MediaItemVM MediaItem { get; }
  public PersonVM Person { get; }
  public SegmentVM Segment { get; }
  public VideoVM Video { get; }
  public ViewerVM Viewer { get; }

  public MainTabsVM MainTabs { get; } = new();
  public MainWindowVM MainWindow { get; }
  public MediaViewerVM MediaViewer { get; } = new();
  public PeopleVM? People { get; set; }
  public SegmentsDrawerVM SegmentsDrawer { get; }
  public TitleProgressBarVM TitleProgressBar { get; } = new();

  public TabControl ToolsTabs => MainWindow.ToolsTabs;
  public static double DisplayScale { get; set; }
  public static IPlatformSpecificUiMediaPlayer UiFullVideo { get; set; } = null!;
  public static IPlatformSpecificUiMediaPlayer UiDetailVideo { get; set; } = null!;
  public static IVideoFrameSaver VideoFrameSaver { get; set; } = null!;

  public event EventHandler AppClosingEvent = delegate { };

  public static RelayCommand AppClosingCommand { get; private set; } = null!;
  public static RelayCommand ExportSegmentsCommand { get; private set; } = null!;
  public static RelayCommand OpenAboutCommand { get; } = new(() => Dialog.Show(new AboutDialog()), null, "About");
  public static RelayCommand OpenLogCommand { get; } = new(() => Dialog.Show(new LogDialog()), MH.UI.Res.IconSort, "Open log");
  public static RelayCommand OpenSegmentsViewsCommand { get; private set; } = null!;
  public static RelayCommand OpenSettingsCommand { get; private set; } = null!;
  public static RelayCommand SaveDbCommand { get; private set; } = null!;
  public static RelayCommand<FolderM> CompressImagesCommand { get; private set; } = null!;
  public static RelayCommand<FolderM> GetGeoNamesFromWebCommand { get; private set; } = null!;
  public static RelayCommand<FolderM> ImagesToVideoCommand { get; private set; } = null!;
  public static RelayCommand<FolderM> ReadGeoLocationFromFilesCommand { get; private set; } = null!;
  public static RelayCommand<FolderM> ReloadMetadataCommand { get; private set; } = null!;
  public static RelayCommand<FolderM> ResizeImagesCommand { get; private set; } = null!;
  public static RelayCommand<FolderM> RotateMediaItemsCommand { get; private set; } = null!;
  public static RelayCommand<FolderM> SaveImageMetadataToFilesCommand { get; private set; } = null!;

  internal CoreVM(CoreS coreS, CoreR coreR) {
    _coreS = coreS;
    _coreR = coreR;

    SegmentVM.SetSegmentUiSize(DisplayScale);
    PersonVM.PersonTileSegmentWidth = SegmentVM.SegmentUiSize + 2;

    MainWindow = new();

    GeoName = new(_coreR.GeoName);
    MediaItem = new(this, _coreS.MediaItem);
    Person = new(this, _coreR.Person);
    Segment = new(this, _coreS.Segment, _coreR.Segment);
    Video = new();
    Viewer = new(_coreR.Viewer, _coreS.Viewer);
    
    SegmentsDrawer = new(_coreS.Segment, _coreR.Segment.Drawer);
    _addToolBars(MainWindow.ToolBar.ToolBars);

    _updateMediaItemsCount();
    _initToggleDialog();

    AppClosingCommand = new(_onAppClosing);
    ExportSegmentsCommand = new(_exportSegments, () => _coreS.Segment.Selected.Items.Any(x => x.MediaItem is ImageM), Res.IconSegment, "Export Segments");
    OpenSettingsCommand = new(_openSettings, Res.IconSettings, "Settings");
    OpenSegmentsViewsCommand = new(() => OpenSegmentsViews(null, string.Empty), Res.IconSegment, "Segments View");
    SaveDbCommand = new(() => _coreR.SaveAllTables(), () => _coreR.Changes > 0, Res.IconDatabase, "Save changes");
    CompressImagesCommand = new(x => _compressImages(GetActive<ImageM>(x)), AnyActive<ImageM>, null, "Compress Images");
    GetGeoNamesFromWebCommand = new(x => _getGeoNamesFromWeb(GetActive<ImageM>(x)), AnyActive<ImageM>, Res.IconLocationCheckin, "Get GeoNames from web");
    ImagesToVideoCommand = new(x => _imagesToVideo(GetActive<ImageM>(x)), AnyActive<ImageM>, null, "Images to Video");
    ReadGeoLocationFromFilesCommand = new(x => _readGeoLocationFromFiles(GetActive<ImageM>(x)), AnyActive<ImageM>, Res.IconLocationCheckin, "Read GeoLocation from files");
    ReloadMetadataCommand = new(x => MediaItem.ReloadMetadata(GetActive<RealMediaItemM>(x)), AnyActive<RealMediaItemM>, null, "Reload metadata");
    ResizeImagesCommand = new(x => _resizeImages(GetActive<ImageM>(x)), AnyActive<ImageM>, null, "Resize Images");
    RotateMediaItemsCommand = new(x => _rotateMediaItems(GetActive<RealMediaItemM>(x)), AnyActive<RealMediaItemM>, null, "Rotate");
    SaveImageMetadataToFilesCommand = new(x => _saveImageMetadataToFiles(GetActive<ImageM>(x)), AnyActive<ImageM>, Res.IconSave, "Save Image metadata to files");
  }

  public bool AnyActive<T>(FolderM? folder = null) where T : MediaItemM =>
    folder != null
    || (MainWindow.IsInViewMode && MediaItem.Current is T)
    || MediaItem.Views.Current?.Selected.Items.OfType<T>().Any() == true;

  public T[] GetActive<T>(FolderM? folder = null) where T : MediaItemM =>
    folder != null
      ? folder.GetMediaItems(Keyboard.IsShiftOn()).OfType<T>().ToArray()
      : MainWindow.IsInViewMode
        ? MediaItem.Current is T current ? [current] : []
        : MediaItem.Views.Current?.Selected.Items.OfType<T>().ToArray() ?? [];

  internal void AttachEvents() {
    MainTabs.PropertyChanged += _onMainTabsPropertyChanged;
    MainTabs.TabClosedEvent += _onMainTabsTabClosed;
    MainWindow.PropertyChanged += _onMainWindowPropertyChanged;
    MediaItem.PropertyChanged += _onMediaItemPropertyChanged;
    MediaItem.Views.PropertyChanged += _onMediaItemViewsPropertyChanged;
    MediaViewer.PropertyChanged += _onMediaViewerPropertyChanged;
    MediaViewer.Slideshow.PropertyChanged += _onMediaViewerSlideshowPropertyChanged;
    MediaViewer.ZoomAndPan.PropertyChanged += _onMediaViewerZoomAndPanPropertyChanged;
    Video.MediaPlayer.MediaEndedEvent += MediaViewer.Slideshow.OnPlayerMediaEnded;
    Video.MediaPlayer.PropertyChanged += _onVideoMediaPlayerPropertyChanged;
    Video.CurrentVideoItems.Selected.ItemsChangedEvent += _onVideoItemsSelectionChanged;

    _coreR.Folder.ItemRenamedEvent += _onFolderRenamed;
    _coreR.Folder.Tree.ItemSelectedEvent += _onFolderTreeItemSelected;

    _coreR.MediaItem.ItemCreatedEvent += _onMediaItemCreated;
    _coreR.MediaItem.ItemRenamedEvent += _onMediaItemRenamed;
    _coreR.MediaItem.ItemsDeletedEvent += _onMediaItemsDeleted;
    _coreR.MediaItem.MetadataChangedEvent += _onMediaItemsMetadataChanged;
    _coreR.MediaItem.OrientationChangedEvent += _onMediaItemsOrientationChanged;

    _coreR.Person.ItemCreatedEvent += _onPersonCreated;
    _coreR.Person.ItemDeletedEvent += _onPersonDeleted;
    _coreR.Person.PersonsKeywordsChangedEvent += _onPersonsKeywordsChanged;

    _coreR.Segment.ItemCreatedEvent += _onSegmentCreated;
    _coreR.Segment.ItemsDeletedEvent += _onSegmentsDeleted;
    _coreR.Segment.SegmentsKeywordsChangedEvent += _onSegmentsKeywordsChanged;
    _coreR.Segment.SegmentsPersonChangedEvent += _onSegmentsPersonChanged;
  }

  private void _onMainTabsPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (e.Is(nameof(MainTabs.Selected)))
      MediaItem.OnViewSelected(MainTabs.Selected?.Data as MediaItemsViewVM);
  }

  private void _onMainTabsTabClosed(object? sender, IListItem tab) {
    switch (tab.Data) {
      case MediaItemsViewVM miView:
        MediaItem.Views.CloseView(miView);
        break;
      case PersonS people:
        people.Selected.DeselectAll();
        break;
      case SegmentsViewsVM sv:
        sv.Close();
        break;
    }
  }

  private void _onMainWindowPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (!e.Is(nameof(MainWindow.IsInViewMode))) return;
    var isInViewMode = MainWindow.IsInViewMode;

    MediaViewer.IsVisible = isInViewMode;

    if (isInViewMode) {
      Video.MediaPlayer.SetView(UiFullVideo);
      _coreS.Segment.Rect.MediaItem = MediaItem.Current;
    }
    else {
      MediaItem.Views.SelectAndScrollToCurrentMediaItem();
      MediaViewer.Deactivate();
      Video.MediaPlayer.SetView(UiDetailVideo);
    }

    MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
  }

  private void _onMediaItemPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (!e.Is(nameof(MediaItem.Current))) return;
    MainWindow.StatusBar.Update();
    Video.SetCurrent(MediaItem.Current);

    if (!MainWindow.IsInViewMode) return;
    MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
    _coreS.Segment.Rect.MediaItem = MediaItem.Current;
  }

  private void _onMediaItemViewsPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (!e.Is(nameof(MediaItem.Views.Current))) return;
    MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
    MainWindow.StatusBar.OnPropertyChanged(nameof(MainWindow.StatusBar.IsCountVisible));
  }

  private void _onMediaViewerPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    switch (e.PropertyName) {
      case nameof(MediaViewer.IsVisible):
        MainWindow.StatusBar.Update();
        MainWindow.StatusBar.OnPropertyChanged(nameof(MainWindow.StatusBar.IsCountVisible));
        break;
      case nameof(MediaViewer.Current):
        if (MediaViewer.Current != null && !ReferenceEquals(MediaItem.Current, MediaViewer.Current))
          MediaItem.Current = MediaViewer.Current;
        else
          Video.SetCurrent(MediaViewer.Current, true);
        break;
    }
  }

  private void _onMediaViewerSlideshowPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (e.Is(nameof(SlideshowVM.State)) && ((SlideshowVM)sender!).State == SlideshowState.On)
      Video.MediaPlayer.PlayType = MediaPlayer.MediaPlayType.Video;
  }

  private void _onMediaViewerZoomAndPanPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (sender is not ZoomAndPan zap) return;
    if (e.Is(nameof(ZoomAndPan.ScaleX)))
      _coreS.Segment.Rect.UpdateScale(zap.ScaleX);
  }

  private void _onVideoItemsSelectionChanged(object? sender, VideoItemM[] e) {
    var vi = e.FirstOrDefault();
    MediaItem.Current = vi as MediaItemM ?? Video.Current;
    Video.MediaPlayer.SetCurrent(vi);
  }

  private void _onVideoMediaPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (!_coreS.Segment.Rect.AreVisible || !MediaViewer.IsVisible ||
        MediaItem.Current == null || !e.Is(nameof(MediaPlayer.TimelinePosition))) return;

    var pos = MediaItem.Current is VideoItemM vi ? vi.TimeStart : 0;
    MediaItemM? mi = Video.MediaPlayer.TimelinePosition == pos ? MediaItem.Current : null;

    if (!ReferenceEquals(mi, _coreS.Segment.Rect.MediaItem))
      _coreS.Segment.Rect.MediaItem = mi;
  }

  private void _onFolderRenamed(object? sender, FolderM item) =>
    MainWindow.StatusBar.UpdateFilePath();

  private void _onFolderTreeItemSelected(object? sender, ITreeItem item) {
    if (MediaViewer.IsVisible)
      MainWindow.IsInViewMode = false;
  }

  private void _onMediaItemCreated(object? sender, MediaItemM item) =>
    _updateMediaItemsCount();

  private void _onMediaItemsDeleted(object? sender, IList<MediaItemM> items) {
    _updateMediaItemsCount();
    MediaItem.Views.RemoveMediaItems(items);
    Video.CurrentVideoItems.Remove(items.OfType<VideoItemM>().ToArray());

    if (!MediaViewer.IsVisible) return;

    if (items.FirstOrDefault() is VideoItemM vi && MediaViewer.MediaItems.Contains(vi.Video))
      MediaItem.Current = vi.Video;
    else
      MediaItem.Current = MediaViewer.MediaItems.GetNextOrPreviousItem(items);

    if (MediaItem.Current == null)
      MainWindow.IsInViewMode = false;
    else
      MediaViewer.Remove(items[0], MediaItem.Current);
  }

  private void _onMediaItemRenamed(object? sender, MediaItemM item) {
    MediaItem.OnPropertyChanged(nameof(MediaItem.Current));
    MediaItem.Views.Current?.OnMediaItemRenamed(item);
  }

  private void _onMediaItemsMetadataChanged(object? sender, MediaItemM[] items) {
    var all = items.OfType<VideoItemM>().Select(x => x.Video).Concat(items).Distinct().ToArray();
    MediaItem.OnMetadataChanged(all);
    MediaItem.Views.UpdateViews(all);
    Video.CurrentVideoItems.Update(items.OfType<VideoItemM>().ToArray());
    MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
    MainWindow.StatusBar.UpdateRating();
  }

  private void _onMediaItemsOrientationChanged(object? sender, RealMediaItemM[] items) {
    if (MediaViewer.IsVisible && items.Contains(MediaViewer.Current))
      MediaViewer.Current = MediaViewer.Current;

    MediaItem.Views.ReWrapViews(items.Cast<MediaItemM>().ToArray());
    if (items.Contains(Video.Current))
      Video.CurrentVideoItems.ReWrapAll();
  }

  private void _onPersonCreated(object? sender, PersonM item) {
    MainWindow.ToolsTabs.PeopleTab?.Insert(item);
    People?.Insert(item);
  }

  private void _onPersonDeleted(object? sender, PersonM item) {
    People?.Remove(item);
    MainWindow.ToolsTabs.PeopleTab?.Remove(item);
    Segment.Views.OnPersonDeleted(item);

    if (ReferenceEquals(MainWindow.ToolsTabs.PersonDetailTab?.PersonM, item))
      MainWindow.ToolsTabs.Close(MainWindow.ToolsTabs.PersonDetailTab);
  }

  private void _onPersonsKeywordsChanged(object? sender, PersonM[] items) {
    MainWindow.ToolsTabs.PersonDetailTab?.UpdateDisplayKeywordsIfContains(items);
    MainWindow.ToolsTabs.PeopleTab?.Update(items);
    People?.Update(items);
    Segment.Views.OnPersonsKeywordsChanged(items);
  }

  private void _onSegmentCreated(object? sender, SegmentM e) =>
    Segment.Views.Current?.Insert(e);

  private void _onSegmentsDeleted(object? sender, IList<SegmentM> items) {
    var arr = items.ToArray();
    MainWindow.ToolsTabs.PersonDetailTab?.Update(arr, true, true);
    Segment.Views.RemoveSegments(arr);
    SegmentsDrawer.RemoveIfContains(arr);
  }

  private void _onSegmentsKeywordsChanged(object? sender, SegmentM[] items) {
    MainWindow.ToolsTabs.PersonDetailTab?.Update(items, true, false);
    Segment.Views.UpdateViews(items);
  }

  private void _onSegmentsPersonChanged(object? sender, (SegmentM[], PersonM?, PersonM[]) e) {
    MainWindow.ToolsTabs.PersonDetailTab?.Update(e.Item1);
    People?.Update(e.Item3);
    Segment.Views.OnSegmentsPersonChanged(e.Item1);
  }

  private void _onAppClosing() {
    AppClosingEvent.Invoke(this, EventArgs.Empty);

    if (_coreR.Changes > 0 &&
        Dialog.Show(new MessageDialog(
          "Database changes",
          "There are some changes in Picture Manager database.\nDo you want to save them?",
          Res.IconDatabase,
          true)) == 1)
      _coreR.SaveAllTables();

    _coreR.BackUp();
  }

  private void _exportSegments() =>
    Dialog.Show(new ExportSegmentsDialog(_coreS.Segment.Selected.Items.Where(x => x.MediaItem is ImageM).ToArray()));

  private void _openSettings() =>
    MainTabs.Activate(Res.IconSettings, "Settings", Core.Inst.AllSettings);

  public void OpenPeopleView() {
    People ??= new();
    People.Reload();
    MainTabs.Activate(Res.IconPeopleMultiple, "People", People);
  }

  public void OpenSegmentsViews(SegmentM[]? segments, string tabTitle) {
    if (segments == null) {
      var result = SegmentsViewsVM.GetSegmentsToLoadUserInput();
      if (result < 1) return;
      segments = SegmentsViewsVM.GetSegments(result).ToArray();
    }
    
    MainTabs.Activate(Res.IconSegment, "Segments", Segment.Views);
    if (MediaViewer.IsVisible) MainWindow.IsInViewMode = false;
    Segment.Views.Load(segments, tabTitle);
  }

  private void _compressImages(ImageM[] items) =>
    Dialog.Show(new CompressImagesDialog(items, _coreS.Image, Core.Settings.Common.JpegQuality));

  private void _getGeoNamesFromWeb(ImageM[] items) {
    if (GetGeoNamesFromWebDialog.Open(items, _coreR))
      _coreR.MediaItem.RaiseMetadataChanged(items.Cast<MediaItemM>().ToArray());
  }

  private void _imagesToVideo(ImageM[] items) {
    if (items.Length < 2) return;
    Dialog.Show(new ImagesToVideoDialog(
      items,
      (folder, fileName) => {
        var mi = _coreR.Video.ItemCreate(folder, fileName);
        var mim = new MediaItemMetadata(mi);
        MediaItemS.ReadMetadata(mim, false);
        if (mim.Success)
          mim.FindRefs().ContinueWith(delegate {
            mi.SetThumbSize();
            MediaItem.Views.Current?.Add(mi);
          }, Tasks.UiTaskScheduler);
      })
    );
  }

  private void _readGeoLocationFromFiles(ImageM[] items) {
    Dialog.Show(new ReadGeoLocationFromFilesDialog(items));
    _coreR.MediaItem.RaiseMetadataChanged(items.Cast<MediaItemM>().ToArray());
  }

  private static void _resizeImages(ImageM[] items) =>
    Dialog.Show(new ImageResizeDialog(items));

  private void _rotateMediaItems(RealMediaItemM[] items) {
    if (RotationDialog.Open(out var rotation))
      _coreR.MediaItem.Rotate(items, rotation);
  }

  private void _saveImageMetadataToFiles(ImageM[] items) {
    SaveMetadataDialog.Open(items, _coreS.Image, Core.Settings.Common.JpegQuality);
    _ = MainWindow.StatusBar.UpdateFileSize();
  }

  private void _updateMediaItemsCount() =>
    MediaItem.ItemsCount = _coreR.Image.All.Count + _coreR.Video.All.Count;

  private void _addToolBars(ObservableCollection<object> toolBars) {
    toolBars.Add(new TitleToolBarVM());
    toolBars.Add(new MiscToolBarVM());
    toolBars.Add(new SegmentToolBarVM(_coreS.Segment));
    toolBars.Add(new PersonToolBarVM());
    toolBars.Add(new SlideshowToolBarVM(MediaViewer.Slideshow));
    toolBars.Add(new MediaItemToolBarVM(MediaItem.Views, MainWindow));
    toolBars.Add(new MediaViewerToolBarVM(MediaViewer));
  }

  private void _initToggleDialog() {
    var ttSegment = new ToggleDialogTargetType<SegmentM>(
      Res.IconSegment,
      _ => _coreS.Segment.Selected.Items.ToArray(),
      count => "{0} Segment{1}".Plural(count));

    var ttPerson = new ToggleDialogTargetType<PersonM>(
      Res.IconPeople,
      _ => _coreS.Person.Selected.Items.ToArray(),
      count => "{0} Person{1}".Plural(count));

    var ttMediaItem = new ToggleDialogTargetType<MediaItemM>(
      MH.UI.Res.IconImage,
      item => item is PersonM person
        ? GetActive<MediaItemM>().Where(mi => mi.Segments?.GetPeople().Contains(person) != true).ToArray()
        : GetActive<MediaItemM>(),
      count => "{0} Media Item{1}".Plural(count));

    var ttVideoItem = new ToggleDialogTargetType<MediaItemM>(
      MH.UI.Res.IconMovieClapper,
      item => item is PersonM person
        ? Video.CurrentVideoItems.Selected.Items.Where(mi => mi.Segments?.GetPeople().Contains(person) != true).Cast<MediaItemM>().ToArray()
        : Video.CurrentVideoItems.Selected.Items.Cast<MediaItemM>().ToArray(),
      count => "{0} Video Item{1}".Plural(count));

    var stKeyword = new ToggleDialogSourceType<KeywordM>(Res.IconTagLabel, "Add/Remove Keyword", "Add or Remove on:");
    stKeyword.Options.Add(new ToggleDialogOption<KeywordM, MediaItemM>(ttMediaItem, _coreR.MediaItem.ToggleKeyword));
    stKeyword.Options.Add(new ToggleDialogOption<KeywordM, MediaItemM>(ttVideoItem, _coreR.MediaItem.ToggleKeyword));
    stKeyword.Options.Add(new ToggleDialogOption<KeywordM, PersonM>(ttPerson, _coreR.Person.ToggleKeyword));
    stKeyword.Options.Add(new ToggleDialogOption<KeywordM, SegmentM>(ttSegment, _coreR.Segment.ToggleKeyword));

    var stPerson = new ToggleDialogSourceType<PersonM>(Res.IconPeople, "Add/Remove Person", "Add or Remove on:");
    stPerson.Options.Add(new ToggleDialogOption<PersonM, MediaItemM>(ttMediaItem, _coreR.MediaItem.TogglePerson));
    stPerson.Options.Add(new ToggleDialogOption<PersonM, MediaItemM>(ttVideoItem, _coreR.MediaItem.TogglePerson));
    stPerson.Options.Add(new ToggleDialogOption<PersonM, SegmentM>(ttSegment, _coreS.Segment.SetSelectedAsPerson));

    var stGeoName = new ToggleDialogSourceType<GeoNameM>(Res.IconLocationCheckin, "Set GeoName", "Set GeoName on:");
    stGeoName.Options.Add(new ToggleDialogOption<GeoNameM, MediaItemM>(ttMediaItem, _coreR.MediaItem.SetGeoName));
    stGeoName.Options.Add(new ToggleDialogOption<GeoNameM, MediaItemM>(ttVideoItem, _coreR.MediaItem.SetGeoName));

    var stRating = new ToggleDialogSourceType<RatingTreeM>(Res.IconStar, "Set Rating", "Set Rating on:");
    stRating.Options.Add(new ToggleDialogOption<RatingTreeM, MediaItemM>(ttMediaItem, (items, item) => _coreR.MediaItem.SetRating(items, item.Rating)));
    stRating.Options.Add(new ToggleDialogOption<RatingTreeM, MediaItemM>(ttVideoItem, (items, item) => _coreR.MediaItem.SetRating(items, item.Rating)));
    
    ToggleDialog.SourceTypes.Add(stKeyword);
    ToggleDialog.SourceTypes.Add(stPerson);
    ToggleDialog.SourceTypes.Add(stGeoName);
    ToggleDialog.SourceTypes.Add(stRating);
  }

  public void OpenMediaItems(MediaItemM[]? items, MediaItemM item) {
    items ??= [item];

    MainWindow.IsInViewMode = true;
    MediaViewer.SetMediaItems(items.ToList(), item);
  }

  public static string? BrowseForFolder() {
    var dir = new FolderBrowserDialog();
    if (Dialog.Show(dir) != 1 || dir.SelectedFolder == null) return null;
    var dirPath = dir.SelectedFolder.FullPath;
    Core.Settings.Common.AddDirectorySelectFolder(dirPath);
    return dirPath;
  }
}