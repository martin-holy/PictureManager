using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Dialogs;
using PictureManager.Common.HelperClasses;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using PictureManager.Common.Repositories;
using PictureManager.Common.Services;
using PictureManager.Common.ViewModels.Entities;
using PictureManager.Interfaces.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common.ViewModels;

public class CoreVM : ObservableObject, ICoreVM {
  private readonly CoreS _coreS;
  private readonly CoreR _coreR;

  public ToggleDialog ToggleDialog { get; } = new(null, null);

  public GeoNameVM GeoName { get; }
  public MediaItemVM MediaItem { get; }
  public PersonVM Person { get; }
  public SegmentVM Segment { get; }
  public VideoVM Video { get; }
  public ViewerVM Viewer { get; }

  public ImageComparerVM ImageComparer { get; } = new();
  public TabControl MainTabs { get; } = new() { CanCloseTabs = true };
  public MainWindowVM MainWindow { get; }
  public MediaViewerVM MediaViewer { get; } = new();
  public PeopleVM People { get; set; }
  public SegmentsDrawerVM SegmentsDrawer { get; }
  public SegmentsMatchingVM SegmentsMatching { get; set; }
  public TitleProgressBarVM TitleProgressBar { get; } = new();

  public TabControl ToolsTabs => MainWindow.ToolsTabs;
  public static double DisplayScale { get; set; }
  public static IPlatformSpecificUiMediaPlayer UiFullVideo { get; set; }
  public static IPlatformSpecificUiMediaPlayer UiDetailVideo { get; set; }
  public static IVideoFrameSaver VideoFrameSaver { get; set; }

  ISegmentVM ICoreVM.Segment => Segment;

  public static RelayCommand AppClosingCommand { get; set; }
  public static RelayCommand OpenAboutCommand { get; } = new(() => Dialog.Show(new AboutDialogM()), null, "About");
  public static RelayCommand OpenLogCommand { get; } = new(() => Dialog.Show(new LogDialogM()), null, "Open log");
  public static RelayCommand OpenSegmentsMatchingCommand { get; set; }
  public static RelayCommand OpenSettingsCommand { get; set; }
  public static RelayCommand SaveDbCommand { get; set; }
  public static RelayCommand<FolderM> CompressImagesCommand { get; set; }
  public static RelayCommand<FolderM> GetGeoNamesFromWebCommand { get; set; }
  public static RelayCommand<FolderM> ImagesToVideoCommand { get; set; }
  public static RelayCommand<FolderM> ReadGeoLocationFromFilesCommand { get; set; }
  public static RelayCommand<FolderM> ReloadMetadataCommand { get; set; }
  public static RelayCommand<FolderM> ResizeImagesCommand { get; set; }
  public static RelayCommand<FolderM> RotateMediaItemsCommand { get; set; }
  public static RelayCommand<FolderM> SaveImageMetadataToFilesCommand { get; set; }

  public CoreVM(CoreS coreS, CoreR coreR) {
    _coreS = coreS;
    _coreR = coreR;

    GeoName = new(_coreR.GeoName);
    MediaItem = new(this, _coreS.MediaItem);
    Person = new(this, _coreR.Person);
    Segment = new(this, _coreS.Segment, _coreR.Segment);
    Video = new();
    Viewer = new(_coreR.Viewer, _coreS.Viewer);

    MainWindow = new(this);
    SegmentsDrawer = new(_coreS.Segment, _coreR.Segment.Drawer);

    UpdateMediaItemsCount();
    InitToggleDialog();

    AppClosingCommand = new(AppClosing);
    OpenSettingsCommand = new(OpenSettings, Res.IconSettings, "Settings");
    OpenSegmentsMatchingCommand = new(() => OpenSegmentsMatching(null), Res.IconSegment, "Segments View");
    SaveDbCommand = new(() => _coreR.SaveAllTables(), () => _coreR.Changes > 0, Res.IconDatabase, "Save changes");
    CompressImagesCommand = new(x => CompressImages(GetActive<ImageM>(x)), AnyActive<ImageM>, null, "Compress Images");
    GetGeoNamesFromWebCommand = new(x => GetGeoNamesFromWeb(GetActive<ImageM>(x)), AnyActive<ImageM>, Res.IconLocationCheckin, "Get GeoNames from web");
    ImagesToVideoCommand = new(x => ImagesToVideo(GetActive<ImageM>(x)), AnyActive<ImageM>, null, "Images to Video");
    ReadGeoLocationFromFilesCommand = new(x => ReadGeoLocationFromFiles(GetActive<ImageM>(x)), AnyActive<ImageM>, Res.IconLocationCheckin, "Read GeoLocation from files");
    ReloadMetadataCommand = new(x => MediaItem.ReloadMetadata(GetActive<RealMediaItemM>(x)), AnyActive<RealMediaItemM>, null, "Reload metadata");
    ResizeImagesCommand = new(x => ResizeImages(GetActive<ImageM>(x)), AnyActive<ImageM>, null, "Resize Images");
    RotateMediaItemsCommand = new(x => RotateMediaItems(GetActive<RealMediaItemM>(x)), AnyActive<RealMediaItemM>, null, "Rotate");
    SaveImageMetadataToFilesCommand = new(x => SaveImageMetadataToFiles(GetActive<ImageM>(x)), AnyActive<ImageM>, Res.IconSave, "Save Image metadata to files");
  }

  public bool AnyActive<T>(FolderM folder = null) where T : MediaItemM =>
    folder != null
    || (MainWindow.IsInViewMode && MediaItem.Current is T)
    || MediaItem.Views.Current?.Selected.Items.OfType<T>().Any() == true;

  public T[] GetActive<T>(FolderM folder = null) where T : MediaItemM =>
    folder != null
      ? folder.GetMediaItems(Keyboard.IsShiftOn()).OfType<T>().ToArray()
      : MainWindow.IsInViewMode
        ? MediaItem.Current is T current ? new[] { current } : Array.Empty<T>()
        : MediaItem.Views.Current?.Selected.Items.OfType<T>().ToArray() ?? Array.Empty<T>();

  public void AttachEvents() {
    MainTabs.PropertyChanged += OnMainTabsPropertyChanged;
    MainTabs.TabClosedEvent += OnMainTabsTabClosed;
    MainWindow.PropertyChanged += OnMainWindowPropertyChanged;
    MediaItem.PropertyChanged += OnMediaItemPropertyChanged;
    MediaItem.Views.PropertyChanged += OnMediaItemViewsPropertyChanged;
    MediaViewer.PropertyChanged += OnMediaViewerPropertyChanged;
    Video.MediaPlayer.RepeatEndedEvent += MediaViewer.OnPlayerRepeatEnded;
    Video.CurrentVideoItems.Selected.ItemsChangedEvent += OnVideoItemsSelectionChanged;

    _coreR.Folder.ItemRenamedEvent += OnFolderRenamed;

    _coreR.MediaItem.ItemCreatedEvent += OnMediaItemCreated;
    _coreR.MediaItem.ItemRenamedEvent += OnMediaItemRenamed;
    _coreR.MediaItem.ItemsDeletedEvent += OnMediaItemsDeleted;
    _coreR.MediaItem.MetadataChangedEvent += OnMediaItemsMetadataChanged;
    _coreR.MediaItem.OrientationChangedEvent += OnMediaItemsOrientationChanged;

    _coreR.Person.ItemCreatedEvent += OnPersonCreated;
    _coreR.Person.ItemDeletedEvent += OnPersonDeleted;
    _coreR.Person.PersonsKeywordsChangedEvent += OnPersonsKeywordsChanged;

    _coreR.Segment.ItemCreatedEvent += OnSegmentCreated;
    _coreR.Segment.ItemsDeletedEvent += OnSegmentsDeleted;
    _coreR.Segment.SegmentsKeywordsChangedEvent += OnSegmentsKeywordsChanged;
    _coreR.Segment.SegmentsPersonChangedEvent += OnSegmentsPersonChanged;
  }

  private void OnMainTabsPropertyChanged(object sender, PropertyChangedEventArgs e) {
    if (e.Is(nameof(MainTabs.Selected)))
      MediaItem.Views.SetCurrentView(MainTabs.Selected?.Data as MediaItemsViewVM);
  }

  private void OnMainTabsTabClosed(IListItem tab) {
    switch (tab.Data) {
      case MediaItemsViewVM miView:
        MediaItem.Views.CloseView(miView);
        break;
      case PersonS people:
        people.Selected.DeselectAll();
        break;
      case Settings settings:
        settings.OnClosing();
        break;
    }
  }

  private void OnMainWindowPropertyChanged(object sender, PropertyChangedEventArgs e) {
    if (e.Is(nameof(MainWindow.IsInViewMode))) {
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
  }

  private void OnMediaItemPropertyChanged(object sender, PropertyChangedEventArgs e) {
    if (e.Is(nameof(MediaItem.Current))) {
      MainWindow.StatusBar.Update();
      Video.SetCurrent(MediaItem.Current);

      if (MainWindow.IsInViewMode) {
        MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
        _coreS.Segment.Rect.MediaItem = MediaItem.Current;
      }
    }
  }

  private void OnMediaItemViewsPropertyChanged(object sender, PropertyChangedEventArgs e) {
    if (e.Is(nameof(MediaItem.Views.Current))) {
      MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
      MainWindow.StatusBar.OnPropertyChanged(nameof(MainWindow.StatusBar.IsCountVisible));
    }
  }

  private void OnMediaViewerPropertyChanged(object sender, PropertyChangedEventArgs e) {
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
      case nameof(MediaViewer.Scale):
        _coreS.Segment.Rect.UpdateScale(MediaViewer.Scale);
        break;
    }
  }

  private void OnVideoItemsSelectionChanged(object sender, VideoItemM[] e) {
    var vi = e.FirstOrDefault();
    MediaItem.Current = (MediaItemM)vi ?? Video.Current;
    Video.MediaPlayer.SetCurrent(vi);
  }

  private void OnFolderRenamed(object sender, FolderM item) {
    MainWindow.StatusBar.UpdateFilePath();
  }

  private void OnMediaItemCreated(object sender, MediaItemM item) {
    UpdateMediaItemsCount();
  }

  private void OnMediaItemsDeleted(object sender, IList<MediaItemM> items) {
    MediaItem.Current = MediaViewer.IsVisible && items.All(x => x is RealMediaItemM)
      ? MediaViewer.MediaItems.GetNextOrPreviousItem(items)
      : items.OfType<VideoItemM>().FirstOrDefault()?.Video;

    UpdateMediaItemsCount();
    MediaItem.Views.RemoveMediaItems(items);
    Video.CurrentVideoItems.Remove(items.OfType<VideoItemM>().ToArray());

    if (MediaViewer.IsVisible) {
      if (MediaItem.Current == null)
        MainWindow.IsInViewMode = false;
      else
        MediaViewer.Remove(items[0], MediaItem.Current);
    }
  }

  private void OnMediaItemRenamed(object sender, MediaItemM item) {
    MediaItem.OnPropertyChanged(nameof(MediaItem.Current));
    MediaItem.Views.Current?.SoftLoad(MediaItem.Views.Current.FilteredItems, true, false);
  }

  private void OnMediaItemsMetadataChanged(object sender, MediaItemM[] items) {
    var all = items.OfType<VideoItemM>().Select(x => x.Video).Concat(items).Distinct().ToArray();
    MediaItem.OnMetadataChanged(all);
    MediaItem.Views.UpdateViews(all);
    Video.CurrentVideoItems.Update(items.OfType<VideoItemM>().ToArray());
    MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
    MainWindow.StatusBar.UpdateRating();
  }

  private void OnMediaItemsOrientationChanged(object sender, RealMediaItemM[] items) {
    if (MediaViewer.IsVisible && items.Contains(MediaViewer.Current))
      MediaViewer.Current = MediaViewer.Current;

    MediaItem.Views.ReWrapViews(items.Cast<MediaItemM>().ToArray());
    if (items.Contains(Video.Current))
      Video.CurrentVideoItems.ReWrapAll();
  }

  private void OnPersonCreated(object sender, PersonM item) {
    MainWindow.ToolsTabs.PeopleTab?.Insert(item);
    People?.Insert(item);
  }

  private void OnPersonDeleted(object sender, PersonM item) {
    People?.Remove(item);
    MainWindow.ToolsTabs.PeopleTab?.Remove(item);
    SegmentsMatching?.CvPeople.Remove(item);

    if (ReferenceEquals(MainWindow.ToolsTabs.PersonDetailTab?.PersonM, item))
      MainWindow.ToolsTabs.Close(MainWindow.ToolsTabs.PersonDetailTab);
  }

  private void OnPersonsKeywordsChanged(object sender, PersonM[] items) {
    MainWindow.ToolsTabs.PersonDetailTab?.UpdateDisplayKeywordsIfContains(items);
    MainWindow.ToolsTabs.PeopleTab?.Update(items);
    People?.Update(items);
    SegmentsMatching?.CvPeople.Update(items);
  }

  private void OnSegmentCreated(object sender, SegmentM e) {
    SegmentsMatching?.CvSegments.Insert(e);
  }

  private void OnSegmentsDeleted(object sender, IList<SegmentM> items) {
    MainWindow.ToolsTabs.PersonDetailTab?.Update(items.ToArray(), true, true);
    SegmentsMatching?.CvSegments.Remove(items.ToArray());
    SegmentsDrawer.RemoveIfContains(items.ToArray());
  }

  private void OnSegmentsKeywordsChanged(object sender, SegmentM[] items) {
    MainWindow.ToolsTabs.PersonDetailTab?.Update(items, true, false);
    SegmentsMatching?.CvSegments.Update(items);
  }

  private void OnSegmentsPersonChanged(object sender, (SegmentM[], PersonM, PersonM[]) e) {
    MainWindow.ToolsTabs.PersonDetailTab?.Update(e.Item1);
    People?.Update(e.Item3);
    SegmentsMatching?.OnSegmentsPersonChanged(e.Item1);
  }

  private void AppClosing() {
    if (_coreR.Changes > 0 &&
        Dialog.Show(new MessageDialog(
          "Database changes",
          "There are some changes in database. Do you want to save them?",
          MH.UI.Res.IconQuestion,
          true)) == 1)
      _coreR.SaveAllTables();

    _coreR.BackUp();
  }

  private static void OpenSettings() =>
    Core.VM.MainTabs.Activate(Res.IconSettings, "Settings", Core.Settings);

  public void OpenPeopleView() {
    People ??= new();
    People.Reload();
    Core.VM.MainTabs.Activate(Res.IconPeopleMultiple, "People", People);
  }

  public void OpenSegmentsMatching(SegmentM[] segments) {
    if (segments == null) {
      var result = SegmentsMatchingVM.GetSegmentsToLoadUserInput();
      if (result < 1) return;
      segments = SegmentsMatchingVM.GetSegments(result).ToArray();
    }
    
    SegmentsMatching ??= new(_coreS.Segment);
    MainTabs.Activate(Res.IconSegment, "Segments", SegmentsMatching);
    if (MediaViewer.IsVisible) MainWindow.IsInViewMode = false;
    SegmentsMatching.Reload(segments);
  }

  private static void CompressImages(ImageM[] items) =>
    Dialog.Show(new CompressDialogM(items, Core.Settings.Common.JpegQuality));

  private void GetGeoNamesFromWeb(ImageM[] items) {
    items = items.Where(x => x.GeoLocation is { } gl && gl.GeoName == null && gl.Lat != null && gl.Lng != null).ToArray();
    if (items.Length == 0) return;
    GeoLocationProgressDialog(items, "Getting GeoNames from web ...", async mi => {
      if (_coreR.GeoName.ApiLimitExceeded) return;
      _coreR.MediaItemGeoLocation.ItemUpdate(new(mi,
        await _coreR.GeoLocation.GetOrCreate(mi.GeoLocation.Lat, mi.GeoLocation.Lng, null, null)));
    });
  }

  private void GeoLocationProgressDialog(ImageM[] items, string msg, Func<ImageM, Task> action) {
    var progress = new ProgressBarSyncDialog(msg, Res.IconLocationCheckin);
    _ = progress.Init(items, null, action, mi => mi.FilePath, null);
    progress.Start();
    _coreR.MediaItem.RaiseMetadataChanged(items.Cast<MediaItemM>().ToArray());
  }

  private void ImagesToVideo(ImageM[] items) {
    if (items.Length == 1) return;
    Dialog.Show(new ImagesToVideoDialogM(
      items,
      (folder, fileName) => {
        var mi = _coreR.Video.ItemCreate(folder, fileName);
        var mim = new MediaItemMetadata(mi);
        MediaItemS.ReadMetadata(mim, false);
        mi.SetThumbSize();
        MediaItem.Views.Current.LoadedItems.Add(mi);
        MediaItem.Views.Current.SoftLoad(MediaItem.Views.Current.LoadedItems, true, true);
      })
    );
  }

  private void ReadGeoLocationFromFiles(ImageM[] items, bool reload = true) {
    GeoLocationProgressDialog(items, "Reading GeoLocations from files ...", async mi => {
      if (!reload && mi.GeoLocation != null) return;
      var mim = new MediaItemMetadata(mi);
      await Task.Run(() => { MediaItemS.ReadMetadata(mim, true); });
      if (mim.Success) await mim.FindGeoLocation(false);
    });
  }

  private void ResizeImages(ImageM[] items) =>
    Dialog.Show(new ResizeImagesDialogM(items));

  private void RotateMediaItems(RealMediaItemM[] items) {
    if (RotationDialogM.Open(out var rotation))
      _coreR.MediaItem.Rotate(items, rotation);
  }

  private void SaveImageMetadataToFiles(ImageM[] items) {
    if (Dialog.Show(new MessageDialog(
          "Save metadata to files",
          "Do you really want to save image metadata to {0} file{1}?".Plural(items.Length),
          MH.UI.Res.IconQuestion,
          true)) != 1) return;

    var progress = new ProgressBarAsyncDialog("Saving metadata to files...", MH.UI.Res.IconImage, true, Environment.ProcessorCount);
    progress.Init(items, null, mi => _coreS.Image.TryWriteMetadata(mi), mi => mi.FilePath, null);
    progress.Start();
    Dialog.Show(progress);
    _ = MainWindow.StatusBar.UpdateFileSize();
  }

  public void UpdateMediaItemsCount() =>
    MediaItem.ItemsCount = _coreR.Image.All.Count + _coreR.Video.All.Count;

  private void InitToggleDialog() {
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
}