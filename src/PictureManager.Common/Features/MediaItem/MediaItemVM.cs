using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Features.Folder;
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

  public MediaItemM? Current {
    get => _current;
    set {
      _current = value;
      _updateCommands();
      OnPropertyChanged();
      OnPropertyChanged(nameof(CurrentGeoName));
    }
  }
  public GeoNameM? CurrentGeoName => Current?.GeoLocation?.GeoName;
  public MediaItemsViewsVM Views { get; } = new();
  public int ItemsCount { get => _itemsCount; set { _itemsCount = value; OnPropertyChanged(); } }

  public static AsyncRelayCommand CommentCommand { get; set; } = null!;
  public static AsyncRelayCommand DeleteCommand { get; set; } = null!;
  public static AsyncRelayCommand<GeoNameM> LoadByGeoNameCommand { get; set; } = null!;
  public static AsyncRelayCommand<KeywordM> LoadByKeywordCommand { get; set; } = null!;
  public static AsyncRelayCommand<PersonM> LoadByPersonCommand { get; set; } = null!;
  public static AsyncRelayCommand LoadByPeopleOrSegmentsCommand { get; set; } = null!;
  public static AsyncRelayCommand RenameCommand { get; set; } = null!;
  public static RelayCommand ViewSelectedCommand { get; set; } = null!;
  public static AsyncRelayCommand<FolderM> CopySelectedToFolderCommand { get; private set; } = null!;
  public static AsyncRelayCommand<FolderM> MoveSelectedToFolderCommand { get; private set; } = null!;

  public MediaItemVM(CoreVM coreVM, MediaItemS s) {
    _coreVM = coreVM;
    _s = s;
    CommentCommand = new(_ => _comment(Current!), () => Current != null, Res.IconNotification, "Comment");
    DeleteCommand = new(_ => Delete(_coreVM.GetActive<MediaItemM>()), () => _coreVM.AnyActive<MediaItemM>(), MH.UI.Res.IconXCross, "Delete");
    LoadByGeoNameCommand = new(_loadBy, Res.IconImageMultiple, "Load Media items");
    LoadByKeywordCommand = new(_loadBy, Res.IconImageMultiple, "Load Media items");
    LoadByPersonCommand = new(_loadBy, Res.IconImageMultiple, "Load Media items");
    LoadByPeopleOrSegmentsCommand = new(_loadByPeopleOrSegments, Res.IconImageMultiple, "Load Media items with selected People or Segments");
    RenameCommand = new(_ => Rename((RealMediaItemM)Current!), () => Current is RealMediaItemM, null, "Rename");
    ViewSelectedCommand = new(_viewSelected, _canViewSelected, Res.IconImageMultiple, "View selected");
    CopySelectedToFolderCommand = new(_copySelectedToFolder, _canCopyMoveSelectedToFolder, Res.IconCopy, "Copy");
    MoveSelectedToFolderCommand = new(_moveSelectedToFolder, _canCopyMoveSelectedToFolder, Res.IconMove, "Move");

    Views.CurrentViewSelectionChangedEvent += (_, _) => _updateCommands();
  }

  private void _updateCommands() {
    CommentCommand.RaiseCanExecuteChanged();
    DeleteCommand.RaiseCanExecuteChanged();
    RenameCommand.RaiseCanExecuteChanged();
    ViewSelectedCommand.RaiseCanExecuteChanged();
  }

  private async Task _comment(MediaItemM mi) {
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

  private Task _loadBy(object? o, CancellationToken token) =>
    _coreVM.MediaItem.Views.LoadByTag(o, token);

  private async Task _loadByPeopleOrSegments(CancellationToken token) {
    var md = new MessageDialog(
      "Load Media items",
      "Do you want to load Media items from selected People or Segments?",
      Res.IconImageMultiple,
      true);

    md.Buttons = [
      new(md.SetResult(1, Res.IconPeople, "People"), true),
      new(md.SetResult(2, Res.IconSegment, "Segments"))
    ];

    var result = await Dialog.ShowAsync(md);
    if (result < 1) return;

    var items = result switch {
      1 => Core.S.Person.Selected.Items.ToArray(),
      2 => Core.S.Segment.Selected.Items.ToArray(),
      _ => Array.Empty<object>()
    };

    if (items.Length == 0) return;

    await _coreVM.MediaItem.Views.LoadByTag(items, token);
  }

  private void _viewSelected() {
    var items = Views.Current!.Selected.Items.ToList();
    _coreVM.MainWindow.IsInViewMode = true;
    _coreVM.MediaViewer.SetMediaItems(items, items[0]);
  }

  private bool _canViewSelected() =>
    Views.Current?.Selected.Items.Count > 1;

  private Task _copySelectedToFolder(FolderM? folder, CancellationToken token) =>
    CopyMoveSelectedToFolder(folder, true);

  private Task _moveSelectedToFolder(FolderM? folder, CancellationToken token) =>
    CopyMoveSelectedToFolder(folder, false);

  private bool _canCopyMoveSelectedToFolder(FolderM? folder) =>
    folder is { IsAccessible: true } && Views.Current?.Selected.Items.OfType<RealMediaItemM>().Any() == true;

  public async Task<bool> CopyMoveSelectedToFolder(FolderM? folder, bool copy) {
    if (folder == null || Views.Current?.Selected.Items.OfType<RealMediaItemM>().ToArray() is not { Length: > 0 } items) return false;
    if (await Dialog.ShowAsync(new MessageDialog(
          $"{(copy ? "Copy" : "Move")} media items",
          $"Do you really want to {(copy ? "copy" : "move")} {"{0} media item{1}".Plural(items.Length)} to\n'{folder.Name}'?",
          MH.UI.Res.IconQuestion,
          true)) != 1)
      return false;

    await CopyMoveU.CopyMoveMediaItems(items, folder, copy ? FileOperationMode.Copy : FileOperationMode.Move);
    return true;
  }

  public async Task<bool> Delete(MediaItemM[] items) {
    if (items.Length == 0 || await Dialog.ShowAsync(new MessageDialog(
          "Delete Confirmation",
          "Do you really want to delete {0} item{1}?".Plural(items.Length),
          MH.UI.Res.IconQuestion,
          true)) != 1) return false;

    _s.DeleteFromDrive(items);
    return true;
  }

  public Task ReloadMetadata(RealMediaItemM[] items) =>
    ReloadMetadataDialog.Open(items, _s);

  public async Task Rename(RealMediaItemM current) {
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

    if (await Dialog.ShowAsync(dlg) != 1) return;
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