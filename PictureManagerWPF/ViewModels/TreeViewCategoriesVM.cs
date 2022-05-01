using System.Windows;
using System.Windows.Input;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;

namespace PictureManager.ViewModels {
  public sealed class TreeViewCategoriesVM {
    private readonly Core _core;
    private readonly AppCore _coreVM;
    
    public TreeViewCategoriesM Model { get; }
    public TreeViewSearchVM TreeViewSearchVM { get; }

    public RelayCommand<MouseButtonEventArgs> SelectCommand { get; }
    public RelayCommand<object> ToggleIsPinnedCommand { get; }

    public TreeViewCategoriesVM(Core core, AppCore coreVM, TreeViewCategoriesM model) {
      _core = core;
      _coreVM = coreVM;
      Model = model;

      TreeViewSearchVM = new(Model.TreeViewSearchM);

      SelectCommand = new(Select);
      ToggleIsPinnedCommand = new(ToggleIsPinned);

      _core.FoldersM.AfterItemCreateEventHandler += (o, _) =>
        Model.ScrollToItem = (ITreeItem)o;
      _core.PeopleM.AfterItemCreateEventHandler += (o, _) =>
        Model.ScrollToItem = (ITreeItem)o;
      _core.KeywordsM.AfterItemCreateEventHandler += (o, _) =>
        Model.ScrollToItem = (ITreeItem)o;

      _core.FoldersM.AfterItemRenameEventHandler += (o, _) => {
        // reload if the folder was selected before
        if (o is FolderM { IsSelected: true } folder)
          Select(folder);
      };
    }

    private void ToggleIsPinned() {
      if (_coreVM.MediaViewerVM.IsVisible)
        Model.IsPinnedInViewer = !Model.IsPinnedInViewer;
      else
        Model.IsPinnedInBrowser = !Model.IsPinnedInBrowser;
    }

    private void Select(MouseButtonEventArgs e) =>
      Select((e.OriginalSource as FrameworkElement)?.DataContext as ITreeItem);

    public void Select(ITreeItem item) {
      // SHIFT key => recursive
      // (Folder, FolderKeyword) => MBL => show, MBL+ctrl => and, MBL+alt => hide

      if (item == null) return;

      if (_core.MediaItemsM.IsEditModeOn && item is RatingTreeM or PersonM or KeywordM or GeoNameM) {
        item.IsSelected = false;
        _core.MediaItemsM.SetMetadata(item);

        return;
      }

      switch (item) {
        case RatingTreeM r:
          _ = _core.ThumbnailsGridsM.Current?.ActivateFilter(r, DisplayFilter.Or);
          break;

        case KeywordM k:
          App.Core.ToggleKeyword(k);
          break;

        case PersonM p:
          _core.SegmentsM.SetPerson(p);
          break;

        case FavoriteFolderM ff:
          if (!_core.FoldersM.IsFolderVisible(ff.Folder)) break;
          ff.Folder.ExpandTo();
          Model.ScrollToItem = ff.Folder;
          break;

        case FolderM:
        case FolderKeywordM:
          if (_coreVM.MediaViewerVM.IsVisible)
            _coreVM.MainWindowVM.IsFullScreen = false;

          var (and, hide, recursive) = InputUtils.GetControlAltShiftModifiers();
          _ = _core.ThumbnailsGridsM.LoadByFolder(item, and, hide, recursive);
          break;

        case ViewerM v:
          _coreVM.MainTabsVM.Activate(_coreVM.ViewerVM.MainTabsItem);
          _coreVM.ViewerVM.Viewer = v;
          break;

        case ITreeCategory cat:
          if (cat is PeopleM) {
            _coreVM.MainTabsVM.Activate(_coreVM.PeopleVM.MainTabsItem);
            _core.PeopleM.ReloadPeopleInGroups();
          }
          break;
      }

      item.IsSelected = false;
    }
  }
}
