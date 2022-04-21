using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Utils;
using MH.Utils.BaseClasses;
using PictureManager.CustomControls;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using PictureManager.ViewModels.Tree;
using ObservableObject = MH.Utils.BaseClasses.ObservableObject;

namespace PictureManager.ViewModels {
  public sealed class ViewerVM : ObservableObject {
    private readonly CatTreeView _catTvCategories;
    private readonly ViewersM _viewersM;
    private readonly CategoryGroupsM _categoryGroupsM;
    private ViewerM _viewer;

    public ViewerM Viewer { get => _viewer; set { _viewer = value; OnPropertyChanged(); Reload(); } }
    public HeaderedListItem<object, string> MainTabsItem { get; }
    public ListBox LbIncludedFolders { get; }
    public ListBox LbExcludedFolders { get; }
    public ListBox LbExcludedKeywords { get; }
    public ObservableCollection<ListItem<CategoryGroupM>> CategoryGroups { get; } = new();
    public RelayCommand<ListItem<CategoryGroupM>> CategoryGroupsSelectCommand { get; }

    public ViewerVM(ViewersM viewersM, CategoryGroupsM categoryGroupsM, CatTreeView catTvCategories) {
      _viewersM = viewersM;
      _categoryGroupsM = categoryGroupsM;
      _catTvCategories = catTvCategories;

      MainTabsItem = new(this, "Viewer");

      LbIncludedFolders = new();
      LbExcludedFolders = new();
      LbExcludedKeywords = new();

      CategoryGroupsSelectCommand = new(CategoryGroupsSelect);

      AttachEvents();
    }

    private void AttachEvents() {
      DragDropFactory.SetDrag(LbIncludedFolders, e => (e.OriginalSource as FrameworkElement)?.DataContext as FolderM);
      DragDropFactory.SetDrag(LbExcludedFolders, e => (e.OriginalSource as FrameworkElement)?.DataContext as FolderM);
      DragDropFactory.SetDrag(LbExcludedKeywords, e => (e.OriginalSource as FrameworkElement)?.DataContext as KeywordM);

      DragDropFactory.SetDrop(
        LbIncludedFolders,
        (e, source, data) => CanDropFolder(e, source, data, true),
        (e, source, data) => DoDropFolder(e, source, data, true));

      DragDropFactory.SetDrop(
        LbExcludedFolders,
        (e, source, data) => CanDropFolder(e, source, data, false),
        (e, source, data) => DoDropFolder(e, source, data, false));

      DragDropFactory.SetDrop(LbExcludedKeywords, CanDropKeyword, DoDropKeyword);
    }

    private void CategoryGroupsSelect(ListItem<CategoryGroupM> item) {
      item.IsSelected = !item.IsSelected;
      _viewersM.ToggleCategoryGroup(Viewer, item.Content.Id);
    }

    private void Reload() {
      ReloadCategoryGroups();

      foreach (var licg in CategoryGroups)
        licg.IsSelected = !Viewer.ExcCatGroupsIds.Contains(licg.Content.Id);
    }

    private void ReloadCategoryGroups() {
      var groups = _categoryGroupsM.All
        .OrderBy(x => x.Category)
        .ThenBy(x => x.Name);

      CategoryGroups.Clear();

      foreach (var cg in groups)
        CategoryGroups.Add(new(cg));
    }

    private DragDropEffects CanDropFolder(DragEventArgs e, object source, object data, bool included) {
      if (FolderToModel(data) is not { } model) return DragDropEffects.None;

      if (source.Equals(_catTvCategories)) {
        return (included
            ? Viewer.IncludedFolders
            : Viewer.ExcludedFolders)
          .Contains(model)
            ? DragDropEffects.None
            : DragDropEffects.Copy;
      }

      if ((source.Equals(LbIncludedFolders) || source.Equals(LbExcludedFolders))
          && (e.Source as FrameworkElement)?.TemplatedParent == source)
        return model.Equals((e.OriginalSource as FrameworkElement)?.DataContext)
          ? DragDropEffects.None
          : DragDropEffects.Move;

      return DragDropEffects.None;
    }

    private void DoDropFolder(DragEventArgs e, object source, object data, bool included) {
      if (source.Equals(_catTvCategories))
        _viewersM.AddFolder(Viewer, FolderToModel(data), included);

      if ((e.Source as FrameworkElement)?.TemplatedParent == source)
        _viewersM.RemoveFolder(Viewer, FolderToModel(data), included);
    }

    private DragDropEffects CanDropKeyword(DragEventArgs e, object source, object data) {
      if (KeywordToModel(data) is not { } model) return DragDropEffects.None;

      if (source.Equals(_catTvCategories))
        return Viewer.ExcludedKeywords.Contains(model)
          ? DragDropEffects.None
          : DragDropEffects.Copy;

      if (source.Equals(LbExcludedKeywords))
        return model.Equals((e.OriginalSource as FrameworkElement)?.DataContext)
          ? DragDropEffects.None
          : DragDropEffects.Move;

      return DragDropEffects.None;
    }

    private void DoDropKeyword(DragEventArgs e, object source, object data) {
      if (source.Equals(_catTvCategories))
        _viewersM.AddKeyword(Viewer, KeywordToModel(data));

      if ((e.Source as FrameworkElement)?.TemplatedParent == source)
        _viewersM.RemoveKeyword(Viewer, KeywordToModel(data));
    }

    private static KeywordM KeywordToModel(object item) =>
      item switch {
        KeywordTreeVM x => x.Model,
        KeywordM x => x,
        _ => null
      };

    private static FolderM FolderToModel(object item) =>
      item switch {
        FolderTreeVM x => x.Model,
        FolderM x => x,
        _ => null
      };
  }
}
