using System.Collections.Generic;
using System.IO;
using System.Linq;
using PictureManager.Dialogs;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Viewer : BaseTreeViewItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; }
    public bool IsDefault { get; set; } 

    public BaseTreeViewItem IncludedFolders { get; }
    public BaseTreeViewItem ExcludedFolders { get; }

    private readonly HashSet<int> _incFoIds;
    private readonly HashSet<int> _incFoTreeIds;
    private readonly HashSet<int> _excFoIds;

    public Viewer(int id, string name, BaseTreeViewItem parent) {
      Id = id;
      Title = name;
      Parent = parent;

      IncludedFolders = new BaseTreeViewItem { Title = "Included Folders", IconName = IconName.FolderStar, Parent = this };
      ExcludedFolders = new BaseTreeViewItem { Title = "Excluded Folders", IconName = IconName.FolderStar, Parent = this };

      Items.Add(IncludedFolders);
      Items.Add(ExcludedFolders);

      IconName = IconName.Eye;

      _incFoIds = new HashSet<int>();
      _incFoTreeIds = new HashSet<int>();
      _excFoIds = new HashSet<int>();
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
      var dir = new FolderBrowserDialog(App.WMain);
      if (!(dir.ShowDialog() ?? true)) return;
      if ((included ? IncludedFolders : ExcludedFolders).Items.Any(x => x.ToolTip.Equals(dir.SelectedPath))) return;

      var folder = App.Core.Folders.GetByPath(dir.SelectedPath.TrimEnd(Path.DirectorySeparatorChar));
      if (folder == null) {
        MessageDialog.Show("Information", @"Select this folder in Folders tree first.", false);
        return;
      }

      AddFolder(folder, included);
      App.Core.Viewers.Helper.Table.SaveToFile();
      App.Core.Folders.AddDrives();
      App.Core.FolderKeywords.Load();
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

      // add IDs of Folder and Folder Tree
      if (included) {
        _incFoIds.Add(folder.Id);
        var fos = new List<BaseTreeViewItem>();
        folder.GetThisAndParentRecursive(ref fos);
        foreach (var fo in fos.OfType<Folder>())
          _incFoTreeIds.Add(fo.Id);
      }
      else {
        _excFoIds.Add(folder.Id);
      }
    }

    public bool CanSeeThisFolder(Folder folder) {
      // If Any part of Test Folder ID matches Any Included Folder ID
      // OR
      // If Any part of Included Folder ID matches Test Folder ID

      var testFos = new List<BaseTreeViewItem>();

      folder.GetThisAndParentRecursive(ref testFos);

      var incContain = testFos.OfType<Folder>().Any(testFo => _incFoIds.Any(incFoId => incFoId == testFo.Id))
                       || _incFoTreeIds.Any(incFoId => incFoId == folder.Id);

      var excContain = testFos.OfType<Folder>().Any(testFo => _excFoIds.Any(excFoId => excFoId == testFo.Id));

      return incContain && !excContain;
    }

    public bool CanSeeContentOfThisFolder(Folder folder) {
      // If Any part of Test Folder ID matches Any Included Folder ID

      var testFos = new List<BaseTreeViewItem>();

      folder.GetThisAndParentRecursive(ref testFos);

      var incContain = testFos.OfType<Folder>().Any(testFo => _incFoIds.Any(incFoId => incFoId == testFo.Id));

      var excContain = testFos.OfType<Folder>().Any(testFo => _excFoIds.Any(excFoId => excFoId == testFo.Id));

      return incContain && !excContain;
    }

    private static void SetInPlace(BaseTreeViewItem item) {
      item.Parent.Items.Add(item);
      var idx = item.Parent.Items.OrderBy(x => x.ToolTip).ToList().IndexOf(item);
      item.Parent.Items.Move(item.Parent.Items.IndexOf(item), idx);
    }
  }
}
