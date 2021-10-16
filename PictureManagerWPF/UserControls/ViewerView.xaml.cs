using PictureManager.Domain.Models;
using PictureManager.Utils;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using PictureManager.ViewModels.Tree;

namespace PictureManager.UserControls {
  public partial class ViewerView : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private Viewer _viewer;
    private string _title;
    private bool _reloading;

    public Viewer Viewer { get => _viewer; set { _viewer = value; OnPropertyChanged(); } }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public static IOrderedEnumerable<CategoryGroupM> CategoryGroups =>
      App.Core.CategoryGroupsM.All.OrderBy(x => x.Category).ThenBy(x => x.Name);

    public ViewerView() {
      InitializeComponent();
      DataContext = this;
      Title = "Viewer";
      AttachEvents();
    }

    private void AttachEvents() {
      DragDropFactory.SetDrag(LbIncludedFolders, (e) => (e.OriginalSource as FrameworkElement)?.DataContext as Folder);
      DragDropFactory.SetDrag(LbExcludedFolders, (e) => (e.OriginalSource as FrameworkElement)?.DataContext as Folder);
      DragDropFactory.SetDrag(LbExcludedKeywords, (e) => (e.OriginalSource as FrameworkElement)?.DataContext as KeywordM);

      DragDropFactory.SetDrop(
        LbIncludedFolders,
        (e, source, data) => CanDropFolder(e, source, data, true),
        (e, source, data) => DoDropFolder(e, source, data, true));

      DragDropFactory.SetDrop(
        LbExcludedFolders,
        (e, source, data) => CanDropFolder(e, source, data, false),
        (e, source, data) => DoDropFolder(e, source, data, false));

      DragDropFactory.SetDrop(LbExcludedKeywords, CanDropKeyword, DoDropKeyword);

      LbCategoryGroups.SelectionChanged += (o, e) => {
        if (_reloading) return;

        foreach (var cg in e.AddedItems.Cast<CategoryGroupTreeVM>().Concat(e.RemovedItems.Cast<CategoryGroupTreeVM>()))
          Viewer.ToggleCategoryGroup(cg.BaseVM.Model.Id);
      };
    }

    private DragDropEffects CanDropFolder(DragEventArgs e, object source, object data, bool included) {
      if (data is not Folder) return DragDropEffects.None;

      if (source == App.WMain.TreeViewCategories.TvCategories) {
        return (included ? Viewer.IncludedFolders : Viewer.ExcludedFolders).Contains(data)
          ? DragDropEffects.None
          : DragDropEffects.Copy;
      }

      if (source == LbIncludedFolders || source == LbExcludedFolders)
        return (e.OriginalSource as FrameworkElement)?.DataContext == data
          ? DragDropEffects.None
          : DragDropEffects.Move;

      return DragDropEffects.None;
    }

    private void DoDropFolder(DragEventArgs e, object source, object data, bool included) {
      if (source == App.WMain.TreeViewCategories.TvCategories)
        Viewer.AddFolder((Folder)data, included);

      if (e.Source == source)
        Viewer.RemoveFolder((Folder)data, included);

      App.Core.Viewers.DataAdapter.IsModified = true;
    }

    private DragDropEffects CanDropKeyword(DragEventArgs e, object source, object data) {
      if (data is not KeywordM) return DragDropEffects.None;

      if (source == App.WMain.TreeViewCategories.TvCategories)
        return Viewer.ExcludedKeywords.Contains(data)
          ? DragDropEffects.None
          : DragDropEffects.Copy;

      if (source == LbExcludedKeywords)
        return (e.OriginalSource as FrameworkElement)?.DataContext == data
          ? DragDropEffects.None
          : DragDropEffects.Move;

      return DragDropEffects.None;
    }

    private void DoDropKeyword(DragEventArgs e, object source, object data) {
      if (source == App.WMain.TreeViewCategories.TvCategories)
        Viewer.AddKeyword((KeywordM)data);

      if (e.Source == source)
        Viewer.RemoveKeyword((KeywordM)data);

      App.Core.Viewers.DataAdapter.IsModified = true;
    }

    public void Reload(Viewer viewer) {
      _reloading = true;
      Viewer = viewer;

      LbCategoryGroups.SelectedItems.Clear();
      foreach (var cg in CategoryGroups.Where(x => !viewer.ExcCatGroupsIds.Contains(x.Id)))
        LbCategoryGroups.SelectedItems.Add(cg);

      _reloading = false;
    }
  }
}