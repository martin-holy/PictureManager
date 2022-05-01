using MH.Utils.Interfaces;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PictureManager.ViewModels {
  public sealed class FoldersVM {
    private readonly Core _core;
    private readonly AppCore _coreVM;

    public FoldersM Model { get; }

    public FoldersVM(Core core, AppCore coreVM, FoldersM model) {
      _core = core;
      _coreVM = coreVM;
      Model = model;

      Model.AfterItemDeleteEventHandler += (o, _) => {
        // delete folder, sub folders and mediaItems from file system
        if (o is FolderM folder && Directory.Exists(folder.FullPath))
          AppCore.FileOperationDelete(new() { folder.FullPath }, true, false);
      };

      Model.OnDropAction = OnDrop;
    }

    public void OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy) {
      if (dest is not FolderM destFolder) return;
      var foMode = copy
        ? FileOperationMode.Copy
        : FileOperationMode.Move;

      switch (src) {
        case FolderM srcData: // Folder
          CopyMoveVM(foMode, srcData, destFolder);

          // reload last selected source if was moved
          if (foMode == FileOperationMode.Move && srcData.IsSelected && destFolder.GetByPath(srcData.Name) != null) {
            destFolder.ExpandTo();
            _coreVM.TreeViewCategoriesVM.Select(destFolder);
          }

          break;

        case string[]: // MediaItems
          _coreVM.MediaItemsVM.CopyMove(foMode,
            _core.ThumbnailsGridsM.Current.FilteredItems.Where(x => x.IsSelected).ToList(),
            destFolder);
          _core.MediaItemsM.DataAdapter.IsModified = true;

          break;
      }

      _core.TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
    }

    private void CopyMoveVM(FileOperationMode mode, FolderM srcFolder, FolderM destFolder) {
      var fop = new FileOperationDialog(Application.Current.MainWindow, mode) { PbProgress = { IsIndeterminate = true } };
      fop.RunTask = Task.Run(() => {
        fop.LoadCts = new();
        var token = fop.LoadCts.Token;

        try {
          Model.CopyMove(mode, srcFolder, destFolder, fop.Progress,
            (string srcFilePath, string destFilePath, ref string destFileName) =>
              AppCore.ShowFileOperationCollisionDialog(srcFilePath, destFilePath, fop, ref destFileName), token);
        }
        catch (Exception ex) {
          ErrorDialog.Show(ex);
        }
      }).ContinueWith(_ => Core.RunOnUiThread(() => fop.Close()));

      fop.ShowDialog();
    }
  }
}
