using System.Threading;
using System.Threading.Tasks;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public static class FoldersViewModel {
    public static void CopyMove(FileOperationMode mode, Folder srcFolder, Folder destFolder) {
      var fop = new FileOperationDialog {Owner = App.WMain, PbProgress = {IsIndeterminate = true}};
      fop.RunTask = Task.Run(() => {
        fop.LoadCts = new CancellationTokenSource();
        var token = fop.LoadCts.Token;

        Folders.CopyMove(mode, srcFolder, destFolder, fop.Progress,
          (string srcFilePath, string destFilePath, ref string destFileName) =>
            AppCore.ShowFileOperationCollisionDialog(srcFilePath, destFilePath, fop, ref destFileName), token);

      }).ContinueWith(task => Core.Instance.RunOnUiThread(() => fop.Close()));

      fop.ShowDialog();
    }
  }
}
