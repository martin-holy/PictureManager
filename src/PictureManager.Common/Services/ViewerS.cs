using MH.Utils.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using PictureManager.Common.Repositories;
using System.Linq;

namespace PictureManager.Common.Services;

public sealed class ViewerS(ViewerR r) : ObservableObject {
  private ViewerM _current;

  public ViewerM Current { get => _current; set { _current = value; OnPropertyChanged(); } }

  public void SetCurrent(ViewerM viewer) {
    if (ReferenceEquals(Current, viewer)) return;
    if (Current != null) Current.IsDefault = false;
    if (viewer != null && !viewer.IsDefault) {
      viewer.IsDefault = true;
      r.IsModified = true;
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