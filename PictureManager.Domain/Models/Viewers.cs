using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.DataAdapters;
using SimpleDB;
using System.Collections.Generic;

namespace PictureManager.Domain.Models {
  public sealed class Viewers : BaseCatTreeViewCategory, ITable {
    public DataAdapter DataAdapter { get; }
    public List<IRecord> All { get; } = new();

    public Viewers(Core core) : base(Category.Viewers) {
      DataAdapter = new ViewersDataAdapter(core, this);
      Title = "Viewers";
      IconName = IconName.Eye;
      CanCreateItems = true;
      CanRenameItems = true;
      CanDeleteItems = true;
    }

    public override bool CanCreateItem(ICatTreeViewItem item) => item is Viewers;

    public override bool CanRenameItem(ICatTreeViewItem item) => item is Viewer;

    public override bool CanDeleteItem(ICatTreeViewItem item) => item is Viewer || item.Parent?.Parent is Viewer;

    public override bool CanSort(ICatTreeViewItem root) => root.Items.Count > 0 && root is ICatTreeViewCategory;

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      var viewer = new Viewer(DataAdapter.GetNextId(), name, root);
      All.Add(viewer);
      CatTreeViewUtils.SetItemInPlace(root, viewer);
      DataAdapter.IsModified = true;

      return viewer;
    }

    public override void ItemDelete(ICatTreeViewItem item) {
      // item can be Viewer or Inc/Excl Folder
      item.Parent.Items.Remove(item);

      // remove Viewer from DB
      if (item is Viewer viewer)
        All.Remove(viewer);

      DataAdapter.IsModified = true;
    }
  }
}
