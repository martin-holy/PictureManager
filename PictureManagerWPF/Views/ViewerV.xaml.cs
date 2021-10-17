using PictureManager.Domain.Models;
using PictureManager.Utils;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using PictureManager.CustomControls;
using PictureManager.ViewModels.Tree;

namespace PictureManager.Views {
  public partial class ViewerV : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged(this, new(name));

    private CatTreeView _catTvCategories;
    private ViewersM _model;
    private ViewerM _viewer;
    private string _title;
    private bool _reloading;

    public ViewerM Viewer { get => _viewer; set { _viewer = value; OnPropertyChanged(); } }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public IOrderedEnumerable<CategoryGroupM> CategoryGroups =>
      App.Core.CategoryGroupsM.All.OrderBy(x => x.Category).ThenBy(x => x.Name);

    public ViewerV() {
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

      LbCategoryGroups.SelectionChanged += (_, e) => {
        if (_reloading) return;

        foreach (var cg in e.AddedItems.Cast<CategoryGroupM>().Concat(e.RemovedItems.Cast<CategoryGroupM>()))
          _model.ToggleCategoryGroup(Viewer, cg.Id);
      };
    }

    private DragDropEffects CanDropFolder(DragEventArgs e, object source, object data, bool included) {
      if (data is not Folder) return DragDropEffects.None;

      if (source.Equals(_catTvCategories)) {
        return (included ? Viewer.IncludedFolders : Viewer.ExcludedFolders).Contains(data)
          ? DragDropEffects.None
          : DragDropEffects.Copy;
      }

      if ((source.Equals(LbIncludedFolders) || source.Equals(LbExcludedFolders)) && e.Source == source)
        return (e.OriginalSource as FrameworkElement)?.DataContext == data
          ? DragDropEffects.None
          : DragDropEffects.Move;

      return DragDropEffects.None;
    }

    private void DoDropFolder(DragEventArgs e, object source, object data, bool included) {
      if (source.Equals(_catTvCategories))
        _model.AddFolder(Viewer, (Folder)data, included);

      if (e.Source == source)
        _model.RemoveFolder(Viewer, (Folder)data, included);
    }

    private DragDropEffects CanDropKeyword(DragEventArgs e, object source, object data) {
      if (ItemToModel(data) is not { } model) return DragDropEffects.None;

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
        _model.AddKeyword(Viewer, ItemToModel(data));

      if (e.Source == source)
        _model.RemoveKeyword(Viewer, ItemToModel(data));
    }

    private static KeywordM ItemToModel(object item) =>
      item switch {
        KeywordTreeVM x => x.BaseVM.Model,
        KeywordM x => x,
        _ => null
      };

    public void Reload(ViewersM model, ViewerM viewer, CatTreeView ctv) {
      _reloading = true;
      _model = model;
      _catTvCategories = ctv;
      Viewer = viewer;

      LbCategoryGroups.SelectedItems.Clear();
      foreach (var cg in CategoryGroups.Where(x => !viewer.ExcCatGroupsIds.Contains(x.Id)))
        LbCategoryGroups.SelectedItems.Add(cg);

      _reloading = false;
    }
  }
}