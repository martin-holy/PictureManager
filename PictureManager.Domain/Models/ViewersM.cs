using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Database;
using PictureManager.Domain.TreeCategories;

namespace PictureManager.Domain.Models;

public sealed class ViewersM : ObservableObject {
  private readonly ViewersDataAdapter _da;
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
  public RelayCommand<ViewerM> UpdateExcludedCategoryGroupsCommand { get; }

  public ViewersM(ViewersDataAdapter da) {
    _da = da;
    TreeCategory = new(_da);
    ViewerDetailM = new(this);
    SetCurrentCommand = new(SetCurrent);
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
    if (Current != null)
      Current.IsDefault = false;

    if (viewer != null)
      viewer.IsDefault = true;
    else
      Current = null;

    var da = Core.Db.Viewers;
    da.Save();
    Core.Instance.SaveDBPrompt();
    Drives.UpdateSerialNumbers();
    da.DB.LoadAllTables(null);
    da.DB.LinkReferences(null);
    da.DB.ClearDataAdapters();
    Core.Instance.AfterInit();
  }

  public bool CanViewerSee(FolderM folder) =>
    Current?.CanSee(folder) != false;

  public bool CanViewerSeeContentOf(FolderM folder) =>
    Current?.CanSeeContentOf(folder) != false;

  public bool CanViewerSee(MediaItemM mediaItem) =>
    Current?.CanSee(mediaItem) != false;
}