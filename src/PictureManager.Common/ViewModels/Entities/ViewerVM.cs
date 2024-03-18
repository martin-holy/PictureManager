using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Common.Models;
using PictureManager.Common.Repositories;
using PictureManager.Common.Services;
using System.Collections.ObjectModel;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Common.ViewModels.Entities;

public sealed class ViewerVM : ObservableObject {
  private readonly ViewerR _r;
  private ViewerM _selected;

  public ExtObservableCollection<ITreeItem> All => _r.Tree.Items;
  public ViewerM Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
  public ObservableCollection<ListItem<CategoryGroupM>> CategoryGroups { get; } = [];

  public CanDragFunc CanDragFolder { get; set; }
  public CanDropFunc CanDropFolderIncluded { get; }
  public DoDropAction DoDropFolderIncluded { get; }
  public CanDropFunc CanDropFolderExcluded { get; }
  public DoDropAction DoDropFolderExcluded { get; }
  public CanDropFunc CanDropKeyword { get; }
  public DoDropAction DoDropKeyword { get; }

  public RelayCommand UpdateExcludedCategoryGroupsCommand { get; }
  public static RelayCommand<ViewerM> SetCurrentCommand { get; set; }

  public ViewerVM(ViewerR r, ViewerS s) {
    _r = r;
    CanDragFolder = source => source is FolderM ? source : null;
    CanDropFolderIncluded = (a, b, c) => CanDropFolder(a, b, c, true);
    CanDropFolderExcluded = (a, b, c) => CanDropFolder(a, b, c, false);
    DoDropFolderIncluded = (a, b) => DoDropFolder(a, b, true);
    DoDropFolderExcluded = (a, b) => DoDropFolder(a, b, false);
    CanDropKeyword = CanDropKeywordMethod;
    DoDropKeyword = DoDropKeywordMethod;

    UpdateExcludedCategoryGroupsCommand = new(UpdateExcludedCategoryGroups);
    SetCurrentCommand = new(s.SetCurrent, Res.IconEye);
  }

  private void UpdateExcludedCategoryGroups() {
    Selected.ExcludedCategoryGroups.Clear();
    foreach (var cg in CategoryGroups.Where(x => !x.IsSelected))
      Selected.ExcludedCategoryGroups.Add(cg.Content);

    _r.IsModified = true;
  }

  public void OpenDetail(ViewerM viewer) {
    if (viewer == null) return;
    Core.VM.MainTabs.Activate(Res.IconEye, "Viewer", this);
    Selected = viewer;

    var groups = Core.R.CategoryGroup.All
      .OrderBy(x => x.Category)
      .ThenBy(x => x.Name)
      .Select(x => new ListItem<CategoryGroupM>(x))
      .ToArray();

    CategoryGroups.Clear();
    foreach (var cg in groups)
      CategoryGroups.Add(cg);

    foreach (var licg in CategoryGroups)
      licg.IsSelected = !Selected.ExcludedCategoryGroups.Contains(licg.Content);
  }

  private DragDropEffects CanDropFolder(object target, object data, bool haveSameOrigin, bool included) {
    if (data is not FolderM folder)
      return DragDropEffects.None;

    if (!haveSameOrigin)
      return (included
          ? Selected.IncludedFolders
          : Selected.ExcludedFolders)
        .Contains(folder)
          ? DragDropEffects.None
          : DragDropEffects.Copy;

    return ReferenceEquals(folder, target)
      ? DragDropEffects.None
      : DragDropEffects.Move;
  }

  private void DoDropFolder(object data, bool haveSameOrigin, bool included) {
    if (haveSameOrigin)
      _r.RemoveFolder(Selected, (FolderM)data, included);
    else
      _r.AddFolder(Selected, (FolderM)data, included);
  }

  private DragDropEffects CanDropKeywordMethod(object target, object data, bool haveSameOrigin) {
    if (data is not KeywordM keyword)
      return DragDropEffects.None;

    if (haveSameOrigin)
      return ReferenceEquals(keyword, target)
        ? DragDropEffects.None
        : DragDropEffects.Move;

    return Selected.ExcludedKeywords.Contains(keyword)
      ? DragDropEffects.None
      : DragDropEffects.Copy;
  }

  private void DoDropKeywordMethod(object data, bool haveSameOrigin) {
    if (haveSameOrigin)
      _r.RemoveKeyword(Selected, (KeywordM)data);
    else
      _r.AddKeyword(Selected, (KeywordM)data);
  }
}