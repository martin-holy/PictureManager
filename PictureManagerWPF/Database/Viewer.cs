using System.IO;
using System.Linq;
using System.Windows.Forms;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Viewer : BaseTreeViewItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; set; }
    public bool IsDefault { get; set; } 

    public BaseTreeViewItem IncludedFolders { get; }
    public BaseTreeViewItem ExcludedFolders { get; }

    public Viewer(int id, string name, BaseTreeViewItem parent) {
      Id = id;
      Title = name;
      Parent = parent;

      IncludedFolders = new BaseTreeViewItem { Title = "Included Folders", IconName = IconName.FolderStar, Parent = this };
      ExcludedFolders = new BaseTreeViewItem { Title = "Excluded Folders", IconName = IconName.FolderStar, Parent = this };

      Items.Add(IncludedFolders);
      Items.Add(ExcludedFolders);

      IconName = IconName.Eye;
    }

    public string ToCsv() {
      // ID|Name|IncludedFolders|ExcludedFolders|IsDefault
      return string.Join("|",
        Id.ToString(),
        Title,
        string.Join(",", IncludedFolders.Items.Select(x => x.Tag)),
        string.Join(",", ExcludedFolders.Items.Select(x => x.Tag)),
        IsDefault ? "1" : string.Empty);
    }

    public void AddFolder(bool included) {
      // TODO vlastni dialog na vyber slozky
      var dir = new FolderBrowserDialog();
      if (dir.ShowDialog() != DialogResult.OK) return;
      if ((included ? IncludedFolders : ExcludedFolders).Items.Any(x => x.ToolTip.Equals(dir.SelectedPath))) return;

      var folder = ACore.Folders.GetByPath(dir.SelectedPath.TrimEnd(Path.DirectorySeparatorChar));
      if (folder == null) {
        Dialogs.MessageDialog.Show("Information", @"Select this folder in Folders tree first.", false);
        return;
      }

      AddFolder(folder, included);
      ACore.Viewers.Helper.Table.SaveToFile();
      ACore.Folders.AddDrives();
      ACore.FolderKeywords.Load();
    }

    public void AddFolder(Folder folder, bool included) {
      var item = new BaseTreeViewItem {
        Tag = folder.Id,
        Title = folder.Title,
        ToolTip = folder.FullPath,
        IconName = IconName.Folder,
        Parent = included ? IncludedFolders : ExcludedFolders
      };

      SetInPlace(item);
    }

    private static void SetInPlace(BaseTreeViewItem item) {
      item.Parent.Items.Add(item);
      var idx = item.Parent.Items.OrderBy(x => x.ToolTip).ToList().IndexOf(item);
      item.Parent.Items.Move(item.Parent.Items.IndexOf(item), idx);
    }
  }
}
