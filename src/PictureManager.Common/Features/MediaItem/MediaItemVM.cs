using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Interfaces;
using PictureManager.Common.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaItemVM : ObservableObject {
  private readonly CoreVM _coreVM;
  private readonly MediaItemS _s;
  private MediaItemM? _current;
  private int _itemsCount;

  public static IImageSourceConverter<MediaItemM> ThumbConverter { get; set; } = null!;

  public MediaItemM? Current { get => _current; set { _current = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentGeoName)); } }
  public GeoNameM? CurrentGeoName => Current?.GeoLocation?.GeoName;
  public MediaItemsViewsVM Views { get; } = new();
  public int ItemsCount { get => _itemsCount; set { _itemsCount = value; OnPropertyChanged(); } }

  public static AsyncRelayCommand CommentCommand { get; set; } = null!;
  public static RelayCommand DeleteCommand { get; set; } = null!;
  public static AsyncRelayCommand<GeoNameM> LoadByGeoNameCommand { get; set; } = null!;
  public static AsyncRelayCommand<KeywordM> LoadByKeywordCommand { get; set; } = null!;
  public static AsyncRelayCommand<PersonM> LoadByPersonCommand { get; set; } = null!;
  public static AsyncRelayCommand LoadByPeopleOrSegmentsCommand { get; set; } = null!;
  public static RelayCommand RenameCommand { get; set; } = null!;
  public static RelayCommand ViewSelectedCommand { get; set; } = null!;

  public MediaItemVM(CoreVM coreVM, MediaItemS s) {
    _coreVM = coreVM;
    _s = s;
    CommentCommand = new(_ => Comment(Current!), () => Current != null, Res.IconNotification, "Comment");
    DeleteCommand = new(() => Delete(_coreVM.GetActive<MediaItemM>()), () => _coreVM.AnyActive<MediaItemM>());
    LoadByGeoNameCommand = new(LoadBy, Res.IconImageMultiple, "Load Media items");
    LoadByKeywordCommand = new(LoadBy, Res.IconImageMultiple, "Load Media items");
    LoadByPersonCommand = new(LoadBy, Res.IconImageMultiple, "Load Media items");
    LoadByPeopleOrSegmentsCommand = new(LoadByPeopleOrSegments, Res.IconImageMultiple, "Load Media items with selected People or Segments");
    RenameCommand = new(() => Rename((RealMediaItemM)Current!), () => Current is RealMediaItemM, null, "Rename");
    ViewSelectedCommand = new(ViewSelected, CanViewSelected, Res.IconImageMultiple, "View selected");
  }

  private async Task Comment(MediaItemM mi) {
    var commentDialog = new InputDialog(
      "Comment",
      "Add a comment.",
      Res.IconNotification,
      mi.Comment,
      x => string.IsNullOrEmpty(x)
        ? "Comment is empty!"
        : x.Length > 256
          ? "Comment is too long!"
          : null);

    if (await Dialog.ShowAsync(commentDialog) == 1)
      _s.SetComment(mi, StringUtils.NormalizeComment(commentDialog.Answer!));
  }

  private Task LoadBy(object? o, CancellationToken token) {
    if (_coreVM.MediaViewer.IsVisible)
      _coreVM.MainWindow.IsInViewMode = false;

    return _coreVM.MediaItem.Views.LoadByTag(o, token);
  }

  private Task LoadByPeopleOrSegments(CancellationToken token) {
    var md = new MessageDialog(
      "Load Media items",
      "Do you want to load Media items from selected People or Segments?",
      Res.IconImageMultiple,
      true);

    md.Buttons = [
      new(md.SetResult(1, Res.IconPeople, "People"), true),
      new(md.SetResult(2, Res.IconSegment, "Segments"))
    ];

    var result = Dialog.Show(md);
    if (result < 1) return Task.CompletedTask;

    var items = result switch {
      1 => Core.S.Person.Selected.Items.ToArray(),
      2 => Core.S.Segment.Selected.Items.ToArray(),
      _ => Array.Empty<object>()
    };

    return items.Length == 0
      ? Task.CompletedTask
      : _coreVM.MediaItem.Views.LoadByTag(items, token);
  }

  private void ViewSelected() {
    var items = Views.Current!.Selected.Items.ToList();
    _coreVM.MainWindow.IsInViewMode = true;
    _coreVM.MediaViewer.SetMediaItems(items, items[0]);
  }

  private bool CanViewSelected() =>
    Views.Current?.Selected.Items.Count > 1;

  public bool Delete(MediaItemM[] items) {
    if (items.Length == 0 || Dialog.Show(new MessageDialog(
          "Delete Confirmation",
          "Do you really want to delete {0} item{1}?".Plural(items.Length),
          MH.UI.Res.IconQuestion,
          true)) != 1) return false;

    _s.DeleteFromDrive(items);
    return true;
  }

  public void ReloadMetadata(RealMediaItemM[] items) =>
    ReloadMetadataDialog.Open(items, _s);

  public void Rename(RealMediaItemM current) {
    var ext = Path.GetExtension(current.FileName);
    var dlg = new InputDialog(
      "Rename",
      "Add a new name.",
      Res.IconNotification,
      Path.GetFileNameWithoutExtension(current.FileName),
      answer => {
        var newFileName = answer + ext;

        if (Path.GetInvalidFileNameChars().Any(x => newFileName.IndexOf(x) != -1))
          return "New file name contains invalid character!";

        if (File.Exists(IOExtensions.PathCombine(current.Folder.FullPath, newFileName)))
          return "New file name already exists!";

        return string.Empty;
      });

    if (Dialog.Show(dlg) != 1) return;
    _s.Rename(current, dlg.Answer + ext);
  }

  public void OnMetadataChanged(MediaItemM[] items) {
    CurrentGeoName?.OnPropertyChanged(nameof(CurrentGeoName.FullName));
    foreach (var mi in items)
      mi.SetInfoBox(true);
  }

  public void OnViewSelected(MediaItemsViewVM? view) {
    Views.SetCurrentView(view);
    Current = Views.Current?.Selected.Items.Count > 0
      ? Views.Current.Selected.Items[0]
      : null;
  }

  public Task ViewMediaItems(MediaItemM[] items, string name, CancellationToken token) =>
    Views.ViewMediaItems(items, name, token);
}