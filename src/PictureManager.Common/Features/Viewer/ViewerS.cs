using MH.Utils.BaseClasses;
using PictureManager.Common.Features.FavoriteFolder;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Person;
using System.Linq;

namespace PictureManager.Common.Features.Viewer;

public sealed class ViewerS(CoreR coreR) : ObservableObject {
  private ViewerM? _current;

  public ViewerM? Current { get => _current; set { _current = value; OnPropertyChanged(); } }

  public void ChangeCurrent(ViewerM? viewer) {
    if (ReferenceEquals(Current, viewer)) return;
    if (Current != null) Current.IsDefault = false;
    foreach (var ff in coreR.FavoriteFolder.All.Where(x => x.IsHidden)) ff.IsHidden = false;
    foreach (var f in coreR.Folder.All.Where(x => x.IsHidden)) f.IsHidden = false;
    foreach (var cg in coreR.CategoryGroup.All.Where(x => x.IsHidden)) cg.IsHidden = false;
    coreR.Viewer.IsModified = true;
    SetCurrent(viewer);
  }

  public void SetCurrent(ViewerM? viewer) {
    if (viewer != null && !viewer.IsDefault) viewer.IsDefault = true;
    Current = viewer;
    Current?.UpdateHashSets();
    coreR.FolderKeyword.Reload();
    if (Current == null) return;

    foreach (var ff in coreR.FavoriteFolder.Tree.Items.Cast<FavoriteFolderM>())
      ff.IsHidden = !CanViewerSee(ff.Folder);

    foreach (var f in coreR.Folder.Tree.Category.Items.Cast<FolderM>()) {
      f.IsExpanded = false;
      f.IsHidden = !CanViewerSee(f);
    }

    foreach (var cg in coreR.CategoryGroup.All)
      cg.IsHidden = Current?.ExcludedCategoryGroups.Contains(cg) == true;
  }

  public bool CanViewerSee(FolderM folder) =>
    Current?.CanSee(folder) != false;

  public bool CanViewerSee(PersonM person) =>
    Current?.CanSee(person) != false;

  public bool CanViewerSee(KeywordM keyword) =>
    Current?.CanSee(keyword) != false;

  public bool CanViewerSeeContentOf(FolderM folder) =>
    Current?.CanSeeContentOf(folder) != false;

  public bool CanViewerSee(MediaItemM mediaItem) =>
    Current?.CanSee(mediaItem) != false;
}