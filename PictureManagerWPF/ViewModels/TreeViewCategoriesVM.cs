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

    public RelayCommand<MouseButtonEventArgs> SelectCommand { get; }

    public TreeViewCategoriesVM(Core core, AppCore coreVM, TreeViewCategoriesM model) {
      _core = core;
      _coreVM = coreVM;
      Model = model;

      SelectCommand = new(Select);

      AttachEvents();
    }

    private void AttachEvents() {
      _core.FoldersM.AfterItemCreateEventHandler += (_, e) =>
        Model.ScrollToItem = e.Data;
      _core.PeopleM.AfterItemCreateEventHandler += (_, e) =>
        Model.ScrollToItem = e.Data;
      _core.KeywordsM.AfterItemCreateEventHandler += (_, e) =>
        Model.ScrollToItem = e.Data;

      _core.FoldersM.AfterItemRenameEventHandler += (_, e) => {
        // reload if the folder was selected before
        if (e.Data is FolderM { IsSelected: true } folder)
          Select(folder);
      };
    }

    private void Select(MouseButtonEventArgs e) =>
      Select((e.OriginalSource as FrameworkElement)?.DataContext as ITreeItem);

    public void Select(ITreeItem item) {
      // SHIFT key => recursive
      // (Folder, FolderKeyword) => MBL => show, MBL+ctrl => and, MBL+alt => hide

      if (item == null) return;

      switch (item) {
        case RatingTreeM r:
          if (_core.MediaItemsM.IsEditModeOn)
            _core.MediaItemsM.SetMetadata(item);
          else
            _ = _core.ThumbnailsGridsM.Current?.ActivateFilter(r, DisplayFilter.Or);
          break;

        case GeoNameM:
          if (_core.MediaItemsM.IsEditModeOn)
            _core.MediaItemsM.SetMetadata(item);
          break;

        case KeywordM k:
          _core.ToggleKeyword(k);
          break;

        case PersonM p:
          _core.TogglePerson(p);
          break;

        case FavoriteFolderM ff:
          if (!_core.FoldersM.IsFolderVisible(ff.Folder)) break;
          ff.Folder.ExpandTo();
          Model.ScrollToItem = ff.Folder;
          break;

        case FolderM:
        case FolderKeywordM:
          if (_core.MediaViewerM.IsVisible)
            _core.MainWindowM.IsFullScreen = false;

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
            _coreVM.PeopleVM.Reload();
          }
          break;
      }
    }
  }
}
