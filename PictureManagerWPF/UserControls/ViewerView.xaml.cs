using PictureManager.Domain.Models;
using PictureManager.Utils;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

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
    public static IOrderedEnumerable<CategoryGroup> CategoryGroups =>
      App.Core.CategoryGroups.All.Cast<CategoryGroup>().OrderBy(x => x.Category).ThenBy(x => x.Title);

    public ViewerView() {
      InitializeComponent();
      DataContext = this;
      Title = "Viewer";
      AttachEvents();
    }

    private void AttachEvents() {
      DragDropFactory.SetDrag(LbIncludedFolders, (e) => (e.OriginalSource as FrameworkElement)?.DataContext as Folder);
      DragDropFactory.SetDrag(LbExcludedFolders, (e) => (e.OriginalSource as FrameworkElement)?.DataContext as Folder);

      DragDropFactory.SetDrop(
        LbIncludedFolders,
        (e, src, data) => CanDrop(e, src, data, true),
        (e, src, data) => DoDrop(e, src, data, true));

      DragDropFactory.SetDrop(
        LbExcludedFolders,
        (e, src, data) => CanDrop(e, src, data, false),
        (e, src, data) => DoDrop(e, src, data, false));

      LbCategoryGroups.SelectionChanged += (o, e) => {
        if (_reloading) return;

        foreach (var cg in e.AddedItems.Cast<CategoryGroup>().Concat(e.RemovedItems.Cast<CategoryGroup>()))
          Viewer.ToggleCategoryGroup(cg.Id);
      };
    }

    private DragDropEffects CanDrop(DragEventArgs e, object source, object data, bool included) {
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

    private void DoDrop(DragEventArgs e, object src, object data, bool included) {
      if (src == App.WMain.TreeViewCategories.TvCategories)
        Viewer.AddFolder((Folder)data, included);

      if (e.Source == src)
        Viewer.RemoveFolder((Folder)data, included);

      App.Core.Sdb.SetModified<Viewers>();
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