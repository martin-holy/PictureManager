using MH.Utils.BaseClasses;
using PictureManager.Domain.Database;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.TreeCategories;
using System.Linq;

namespace PictureManager.Domain.Models;

public sealed class ViewersM : ObservableObject {
  private readonly ViewersDA _da;
  private ViewerM _current;
  private ViewerM _selected;

  public ViewersTreeCategory TreeCategory { get; }
  public ViewerDetailM ViewerDetailM { get; }
  public ViewerM Current { get => _current; set { _current = value; OnPropertyChanged(); } }
  public ViewerM Selected {
    get => _selected;
    set {
      _selected = value;
      OnPropertyChanged();
      value.Reload(Core.Db.CategoryGroups.All);
    }
  }

  public RelayCommand<ViewerM> SetCurrentCommand { get; }
  public RelayCommand UpdateExcludedCategoryGroupsCommand { get; }

  public ViewersM(ViewersDA da) {
    _da = da;
    TreeCategory = new(_da);
    ViewerDetailM = new(_da);
    SetCurrentCommand = new(SetCurrent, Res.IconEye);
    UpdateExcludedCategoryGroupsCommand = new(UpdateExcludedCategoryGroups);
  }

  private void UpdateExcludedCategoryGroups() {
    Selected.UpdateExcludedCategoryGroups();
    _da.IsModified = true;
  }

  public void OpenDetail(ViewerM viewer) {
    if (viewer == null) return;
    Core.MainTabs.Activate(Res.IconEye, "Viewer", ViewerDetailM);
    Selected = viewer;
  }

  public void SetCurrent(ViewerM viewer) {
    if (ReferenceEquals(Current, viewer)) return;
    if (Current != null) Current.IsDefault = false;
    if (viewer != null) viewer.IsDefault = true;
    _da.IsModified = true;
    Current = viewer;

    foreach (var f in Core.Db.Folders.All.Where(x => x.IsHidden)) f.IsHidden = false;
    foreach (var cg in Core.Db.CategoryGroups.All.Where(x => x.IsHidden)) cg.IsHidden = false;
    if (Current == null) return;

    Current.UpdateHashSets();
    Core.Db.FolderKeywords.Reload();

    foreach (var f in Core.FoldersM.TreeCategory.Items.Cast<FolderM>()) {
      f.IsExpanded = false;
      f.IsHidden = !CanViewerSee(f);
    }

    foreach (var cg in Core.Db.CategoryGroups.All)
      cg.IsHidden = Current?.ExcludedCategoryGroups.Contains(cg) == true;
  }

  public bool CanViewerSee(FolderM folder) =>
    Current?.CanSee(folder) != false;

  public bool CanViewerSeeContentOf(FolderM folder) =>
    Current?.CanSeeContentOf(folder) != false;

  public bool CanViewerSee(MediaItemM mediaItem) =>
    Current?.CanSee(mediaItem) != false;
}