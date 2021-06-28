using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class Viewers : BaseCatTreeViewCategory, ITable, ICatTreeViewCategory {
    public TableHelper Helper { get; set; }
    public List<IRecord> All { get; } = new();

    public Viewers() : base(Category.Viewers) {
      Title = "Viewers";
      IconName = IconName.Eye;
      CanCreateItems = true;
      CanRenameItems = true;
      CanDeleteItems = true;
    }

    public void LoadFromFile() {
      All.Clear();
      Helper.LoadFromFile();
    }

    public void NewFromCsv(string csv) {
      // ID|Name|IncludedFolders|ExcludedFolders|IsDefault
      var props = csv.Split('|');
      if (props.Length != 5) throw new ArgumentException("Incorrect number of values.", csv);
      var viewer = new Viewer(int.Parse(props[0]), props[1], this) { Csv = props, IsDefault = props[4] == "1" };
      if (viewer.IsDefault) Core.Instance.CurrentViewer = viewer;
      All.Add(viewer);
    }

    public void LinkReferences() {
      // ID|Name|IncludedFolders|ExcludedFolders|IsDefault

      Items.Clear();

      foreach (var viewer in All.Cast<Viewer>().OrderBy(x => x.Title)) {
        // reference to IncludedFolders
        if (!string.IsNullOrEmpty(viewer.Csv[2]))
          foreach (var folderId in viewer.Csv[2].Split(',')) {
            var f = Core.Instance.Folders.AllDic[int.Parse(folderId)];
            viewer.AddFolder(f, true);
          }

        // reference to ExcludedFolders
        if (!string.IsNullOrEmpty(viewer.Csv[3]))
          foreach (var folderId in viewer.Csv[3].Split(',')) {
            var f = Core.Instance.Folders.AllDic[int.Parse(folderId)];
            viewer.AddFolder(f, false);
          }

        // adding Viewer to Viewers
        Items.Add(viewer);

        // csv array is not needed any more
        viewer.Csv = null;
      }
    }

    public override bool CanCreateItem(ICatTreeViewItem item) => item is Viewers;

    public override bool CanRenameItem(ICatTreeViewItem item) => item is Viewer;

    public override bool CanDeleteItem(ICatTreeViewItem item) => item is Viewer || item.Parent?.Parent is Viewer;

    public override bool CanSort(ICatTreeViewItem root) => root.Items.Count > 0 && root is ICatTreeViewCategory;

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      var viewer = new Viewer(Helper.GetNextId(), name, root);
      All.Add(viewer);
      CatTreeViewUtils.SetItemInPlace(root, viewer);
      Core.Instance.Sdb.SetModified<Viewers>();
      Core.Instance.Sdb.SaveIdSequences();

      return viewer;
    }

    public override void ItemDelete(ICatTreeViewItem item) {
      // item can be Viewer or Inc/Excl Folder
      item.Parent.Items.Remove(item);

      // remove Viewer from DB
      if (item is Viewer viewer)
        All.Remove(viewer);

      Core.Instance.Sdb.SetModified<Viewers>();
    }
  }
}
