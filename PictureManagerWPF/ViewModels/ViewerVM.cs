using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MH.UI.WPF.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class ViewerVM : ObservableObject {
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

    public ViewerVM(ViewersM viewersM, CategoryGroupsM categoryGroupsM) {
      _viewersM = viewersM;
      _categoryGroupsM = categoryGroupsM;

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
        CategoryGroups.Add(new ListItem<CategoryGroupM>(cg));
    }

    private DragDropEffects CanDropFolder(DragEventArgs e, object source, object data, bool included) {
      if (data is not FolderM folder)
        return DragDropEffects.None;

      if (!source.Equals(LbIncludedFolders) && !source.Equals(LbExcludedFolders))
        return (included
          ? Viewer.IncludedFolders
          : Viewer.ExcludedFolders)
          .Contains(folder)
            ? DragDropEffects.None
            : DragDropEffects.Copy;

      if ((e.Source as FrameworkElement)?.TemplatedParent == source)
        return folder.Equals((e.OriginalSource as FrameworkElement)?.DataContext)
          ? DragDropEffects.None
          : DragDropEffects.Move;

      return DragDropEffects.None;
    }

    private void DoDropFolder(DragEventArgs e, object source, object data, bool included) {
      if ((e.Source as FrameworkElement)?.TemplatedParent == source)
        _viewersM.RemoveFolder(Viewer, (FolderM)data, included);
      else
        _viewersM.AddFolder(Viewer, (FolderM)data, included);
    }

    private DragDropEffects CanDropKeyword(DragEventArgs e, object source, object data) {
      if (data is not KeywordM keyword)
        return DragDropEffects.None;

      if ((e.Source as FrameworkElement)?.TemplatedParent == source)
        return keyword.Equals((e.OriginalSource as FrameworkElement)?.DataContext)
          ? DragDropEffects.None
          : DragDropEffects.Move;
      else
        return Viewer.ExcludedKeywords.Contains(keyword)
          ? DragDropEffects.None
          : DragDropEffects.Copy;
    }

    private void DoDropKeyword(DragEventArgs e, object source, object data) {
      if ((e.Source as FrameworkElement)?.TemplatedParent == source)
        _viewersM.RemoveKeyword(Viewer, (KeywordM)data);
      else
        _viewersM.AddKeyword(Viewer, (KeywordM)data);
    }
  }
}
