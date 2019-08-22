using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Viewers : BaseCategoryItem, ITable {
    public TableHelper Helper { get; set; }
    public List<Viewer> All { get; } = new List<Viewer>();

    public Viewers() : base(Category.Viewers) {
      Title = "Viewers";
      IconName = IconName.Eye;
      IsExpanded = true;
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void LoadFromFile() {
      All.Clear();
      Helper.LoadFromFile();
    }

    public void NewFromCsv(string csv) {
      // ID|Name|IncludedFolders|ExcludedFolders|IsDefault
      var props = csv.Split('|');
      if (props.Length != 5) return;
      var id = int.Parse(props[0]);
      var viewer = new Viewer(id, props[1], this) {Csv = props, IsDefault = props[4] == "1"};
      if (viewer.IsDefault) App.Core.CurrentViewer = viewer;
      AddRecord(viewer);
    }

    public void LinkReferences() {
      // ID|Name|IncludedFolders|ExcludedFolders|IsDefault

      Items.Clear();

      foreach (var viewer in All.OrderBy(x => x.Title)) {
        // reference to IncludedFolders
        if (!string.IsNullOrEmpty(viewer.Csv[2]))
          foreach (var folderId in viewer.Csv[2].Split(',')) {
            var f = App.Core.Folders.AllDic[int.Parse(folderId)];
            viewer.AddFolder(f, true);
          }

        // reference to ExcludedFolders
        if (!string.IsNullOrEmpty(viewer.Csv[3]))
          foreach (var folderId in viewer.Csv[3].Split(',')) {
            var f = App.Core.Folders.AllDic[int.Parse(folderId)];
            viewer.AddFolder(f, false);
          }

        // adding Viewer to Viewers
        Items.Add(viewer);

        // csv array is not needed any more
        viewer.Csv = null;
      }
    }

    private void AddRecord(Viewer record) {
      All.Add(record);
    }

    private void CreateViewer(string name) {
      var viewer = new Viewer(Helper.GetNextId(), name, this);
      AddRecord(viewer);
      App.Core.Sdb.SaveAllTables();
      ItemSetInPlace(this, true, viewer);
      App.WMain.MenuViewers.Visibility = Visibility.Visible;
    }

    public override void ItemNewOrRename(BaseTreeViewItem item, bool rename) {
      var inputDialog = ItemGetInputDialog(item, IconName.Eye, "Viewer", rename);
      inputDialog.BtnDialogOk.Click += delegate {
        if (rename && string.Compare(inputDialog.Answer, item.Title, StringComparison.OrdinalIgnoreCase) == 0) {
          inputDialog.DialogResult = true;
          return;
        }

        if (All.SingleOrDefault(x => x.Title.Equals(inputDialog.Answer)) != null) {
          inputDialog.ShowErrorMessage("This viewer already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      if (!(inputDialog.ShowDialog() ?? true)) return;
      if (rename) {
        var viewer = (Viewer)item;
        viewer.Title = inputDialog.Answer;
        ItemSetInPlace(viewer.Parent, false, viewer);
        Helper.IsModifed = true;
      }
      else CreateViewer(inputDialog.Answer);
    }

    public override void ItemDelete(BaseTreeViewItem item) {
      if (!(item is Viewer viewer)) return;
      
      // remove Viewer from the tree
      item.Parent.Items.Remove(viewer);

      // remove Viewer from DB
      All.Remove(viewer);
      Helper.IsModifed = true;

      // Collapse Viewers menu on title bar if Viewers == 0
      if (Items.Count == 0) App.WMain.MenuViewers.Visibility = Visibility.Collapsed;
    }

    public static void RemoveFolder(BaseTreeViewItem folder) {
      folder.Parent.Items.Remove(folder);
    }

    public static bool CanViewerSeeThisFile(Viewer viewer, string filePath) {
      bool ok;
      if (viewer == null) return true;

      var incFo = viewer.IncludedFolders.Items.Select(x => x.ToolTip).ToArray();
      var excFo = viewer.ExcludedFolders.Items.Select(x => x.ToolTip).ToArray();
      var incFi = new string[0];
      var excFi = new string[0];

      if (incFo.Any(x => filePath.StartsWith(x, StringComparison.OrdinalIgnoreCase))) {
        if (excFo.Any(x => filePath.StartsWith(x, StringComparison.OrdinalIgnoreCase))) {
          ok = incFi.Any(x => filePath.Equals(x, StringComparison.OrdinalIgnoreCase));
        }
        else {
          ok = !excFi.Any(x => filePath.Equals(x, StringComparison.OrdinalIgnoreCase));
        }
      }
      else {
        ok = incFi.Any(x => filePath.Equals(x, StringComparison.OrdinalIgnoreCase));
      }

      return ok;
    }

    // TODO predelat na objekty
    public static bool CanViewerSeeThisDirectory(Viewer viewer, Folder folder) {
      if (viewer == null) return true;

      var path = folder.FullPath;
      bool ok;
      var incFo = viewer.IncludedFolders.Items.Select(x => x.ToolTip).ToArray();
      var excFo = viewer.ExcludedFolders.Items.Select(x => x.ToolTip).ToArray();
      var incFi = new string[0];
      var excFi = new string[0];

      if (incFo.Any(x => x.Contains(path)) || incFo.Any(path.Contains)) {
        if (excFo.Any(x => x.Contains(path)) || excFo.Any(path.Contains)) {
          ok = incFi.Any(x => x.StartsWith(path));
        }
        else {
          ok = !excFi.Any(x => x.StartsWith(path));
        }
      }
      else {
        ok = incFi.Any(x => x.StartsWith(path));
      }

      return ok;
    }
  }
}
