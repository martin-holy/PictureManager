using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.Repositories;
using System.Linq;

namespace PictureManager.Domain.Services;

public sealed class ViewerS : ObservableObject {
  private readonly ViewerR _r;
  private ViewerM _current;
  private ViewerM _selected;

  public ViewerDetailM ViewerDetailM { get; }

  public ViewerM Current {
    get => _current;
    set {
      _current = value;
      OnPropertyChanged();
    }
  }

  public ViewerM Selected {
    get => _selected;
    set {
      _selected = value;
      OnPropertyChanged();
      value.Reload(Core.R.CategoryGroup.All);
    }
  }

  public static RelayCommand<ViewerM> SetCurrentCommand { get; set; }
  public RelayCommand UpdateExcludedCategoryGroupsCommand { get; }

  public ViewerS(ViewerR r) {
    _r = r;
    ViewerDetailM = new(_r, this);
    SetCurrentCommand = new(SetCurrent, Res.IconEye);
    UpdateExcludedCategoryGroupsCommand = new(UpdateExcludedCategoryGroups);
  }

  private void UpdateExcludedCategoryGroups() {
    Selected.UpdateExcludedCategoryGroups();
    _r.IsModified = true;
  }

  public void OpenDetail(ViewerM viewer) {
    if (viewer == null) return;
    Core.MainTabs.Activate(Res.IconEye, "Viewer", ViewerDetailM);
    Selected = viewer;
  }

  public void SetCurrent(ViewerM viewer) {
    if (ReferenceEquals(Current, viewer)) return;
    if (Current != null) Current.IsDefault = false;
    if (viewer != null && !viewer.IsDefault) {
      viewer.IsDefault = true;
      _r.IsModified = true;
    }
    
    Current = viewer;
    foreach (var f in Core.R.Folder.All.Where(x => x.IsHidden)) f.IsHidden = false;
    foreach (var cg in Core.R.CategoryGroup.All.Where(x => x.IsHidden)) cg.IsHidden = false;
    if (Current == null) return;

    Current.UpdateHashSets();
    Core.R.FolderKeyword.Reload();

    foreach (var f in Core.R.Folder.Tree.Items.Cast<FolderM>()) {
      f.IsExpanded = false;
      f.IsHidden = !CanViewerSee(f);
    }

    foreach (var cg in Core.R.CategoryGroup.All)
      cg.IsHidden = Current?.ExcludedCategoryGroups.Contains(cg) == true;
  }

  public bool CanViewerSee(FolderM folder) =>
    Current?.CanSee(folder) != false;

  public bool CanViewerSeeContentOf(FolderM folder) =>
    Current?.CanSeeContentOf(folder) != false;

  public bool CanViewerSee(MediaItemM mediaItem) =>
    Current?.CanSee(mediaItem) != false;
}