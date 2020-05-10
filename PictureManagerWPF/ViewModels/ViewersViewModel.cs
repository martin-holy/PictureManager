using System.IO;
using System.Linq;
using PictureManager.Dialogs;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public static class ViewersViewModel {
    public static void AddFolder(Viewer viewer, bool included) {
      var dir = new FolderBrowserDialog(App.WMain);
      if (!(dir.ShowDialog() ?? true)) return;
      if ((included ? viewer.IncludedFolders : viewer.ExcludedFolders).Items.Any(x => x.ToolTip.Equals(dir.SelectedPath))) return;

      var folder = App.Core.Model.Folders.GetByPath(dir.SelectedPath.TrimEnd(Path.DirectorySeparatorChar));
      if (folder == null) {
        MessageDialog.Show("Information", "Select this folder in Folders tree first.", false);
        return;
      }

      viewer.AddFolder(folder, included);
      App.Core.Model.Viewers.Helper.Table.SaveToFile();
      App.Core.Model.Folders.AddDrives();
      App.Core.Model.FolderKeywords.Load();
    }
  }
}
