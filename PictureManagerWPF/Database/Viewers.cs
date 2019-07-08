




//TODO vsechno




using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VM = PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Viewers : VM.BaseCategoryItem, ITable {
    public Dictionary<int, IRecord> Records { get; set; } = new Dictionary<int, IRecord>();

    public Viewers() : base(Category.Viewers) {
      Title = "Viewers";
      IconName = IconName.Eye;
    }

    public void NewFromCsv(string csv) {
      //TODO
      /*var props = csv.Split('|');
      if (props.Length != 5) return;
      var id = int.Parse(props[0]);
      Records.Add(id, new Keyword(id, props[1], null, int.Parse(props[3])) { Csv = props });*/
    }

    public void LinkReferences(SimpleDB sdb) {
      //TODO
      /*foreach (var item in Records) {
        var keyword = (Keyword)item.Value;

        // reference to parent
        if (keyword.Csv[2] != string.Empty)
          keyword.Parent = (Keyword)Records[int.Parse(keyword.Csv[2])];

        // reference to childrens
        if (keyword.Csv[4] != string.Empty)
          foreach (var keywordId in keyword.Csv[4].Split(','))
            keyword.Items.Add((Keyword)Records[int.Parse(keywordId)]);

        // csv array is not needed any more
        keyword.Csv = null;
      }*/
    }

    public void Load() {
      Items.Clear();

      foreach (var viewer in ACore.Db.Viewers.OrderBy(x => x.Name).Select(x => new Viewer(x))) {
        viewer.Parent = this;
        Items.Add(viewer);
      }
      IsExpanded = true;

      if (Items.Count == 0) AppCore.WMain.MenuViewers.Visibility = Visibility.Collapsed;
    }

    public void CreateViewer(string name) {
      var dmViewer = new DataModel.Viewer {
        Id = ACore.Db.GetNextIdFor<DataModel.Viewer>(),
        Name = name
      };

      ACore.Db.Insert(dmViewer);

      var vmViewer = new Viewer(dmViewer);
      ACore.Viewers.ItemSetInPlace(this, true, vmViewer);
      AppCore.WMain.MenuViewers.Visibility = Visibility.Visible;
    }

    public override void ItemNewOrRename(VM.BaseTreeViewItem item, bool rename) {
      var inputDialog = ItemGetInputDialog(item, IconName.Eye, "Viewer", rename);
      inputDialog.BtnDialogOk.Click += delegate {
        if (rename && string.Compare(inputDialog.Answer, item.Title, StringComparison.OrdinalIgnoreCase) == 0) {
          inputDialog.DialogResult = true;
          return;
        }

        if (ACore.Db.Viewers.SingleOrDefault(x => x.Name.Equals(inputDialog.Answer)) != null) {
          inputDialog.ShowErrorMessage("This viewer already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      if (!(inputDialog.ShowDialog() ?? true)) return;
      if (rename) {
        var viewer = (Viewer)item;
        viewer.Title = inputDialog.Answer;
        ACore.Db.Update(viewer.Data);
        ACore.Viewers.ItemSetInPlace(viewer.Parent, false, viewer);
      }
      else CreateViewer(inputDialog.Answer);
    }

    public override void ItemDelete(VM.BaseTreeViewItem item) {
      if (!(item is Viewer viewer)) return;
      var lists = DataModel.PmDataContext.GetInsertUpdateDeleteLists();

      foreach (var v in ACore.Db.ViewersAccess.Where(x => x.ViewerId == viewer.Data.Id)) {
        DataModel.PmDataContext.DeleteOnSubmit(v, lists);
      }

      DataModel.PmDataContext.DeleteOnSubmit(viewer.Data, lists);
      ACore.Db.SubmitChanges(lists);

      item.Parent.Items.Remove(viewer);
      if (Items.Count == 0) AppCore.WMain.MenuViewers.Visibility = Visibility.Collapsed;
    }

    public void RemoveFolder(VM.BaseTreeViewItem folder) {
      if (!(folder.Tag is DataModel.ViewerAccess data)) return;

      var viewer = Items.Cast<Viewer>().SingleOrDefault(x => x.Data.Id == data.ViewerId);
      if (viewer == null) return;

      ACore.Db.Delete(data);
      (data.IsIncluded ? viewer.IncludedFolders : viewer.ExcludedFolders).Items.Remove(folder);
    }
  }
}
