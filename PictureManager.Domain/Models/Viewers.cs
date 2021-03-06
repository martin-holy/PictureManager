﻿using System.Collections.Generic;
using System.Linq;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class Viewers : BaseCategoryItem, ITable, ICategoryItem {
    public TableHelper Helper { get; set; }
    public List<Viewer> All { get; } = new List<Viewer>();

    public Viewers() : base(Category.Viewers) {
      Title = "Viewers";
      IconName = IconName.Eye;
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
      if (viewer.IsDefault) Core.Instance.CurrentViewer = viewer;
      AddRecord(viewer);
    }

    public void LinkReferences() {
      // ID|Name|IncludedFolders|ExcludedFolders|IsDefault

      Items.Clear();

      foreach (var viewer in All.OrderBy(x => x.Title)) {
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

    private void AddRecord(Viewer record) {
      All.Add(record);
    }

    private void CreateViewer(string name) {
      var viewer = new Viewer(Helper.GetNextId(), name, this);
      AddRecord(viewer);
      Core.Instance.Sdb.SaveAllTables();
      ItemSetInPlace(this, true, viewer);
    }

    public string ValidateNewItemTitle(BaseTreeViewItem root, string name) {
      return All.SingleOrDefault(x => x.Title.Equals(name)) != null
        ? $"{name} viewer already exists!"
        : null;
    }

    public void ItemCreate(BaseTreeViewItem root, string name) {
      CreateViewer(name);
    }

    public void ItemRename(BaseTreeViewItem item, string name) {
      item.Title = name;
      ItemSetInPlace(item.Parent, false, item);
      SaveToFile();
    }

    public void ItemDelete(BaseTreeViewItem item) {
      if (!(item is Viewer viewer)) return;
      
      // remove Viewer from the tree
      item.Parent.Items.Remove(viewer);

      // remove Viewer from DB
      All.Remove(viewer);
      SaveToFile();
    }
  }
}
