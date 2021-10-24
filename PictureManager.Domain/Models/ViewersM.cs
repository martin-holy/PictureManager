using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.EventsArgs;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class ViewersM : ITreeBranch {
    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    public DataAdapter DataAdapter { get; }
    public List<ViewerM> All { get; } = new();

    public event EventHandler<ViewerDeletedEventArgs> ViewerDeletedEvent = delegate { };

    public ViewersM(Core core) {
      DataAdapter = new ViewersDataAdapter(core, this);
    }

    public ViewerM ItemCreate(ITreeBranch root, string name) {
      var item = new ViewerM(DataAdapter.GetNextId(), name, root);
      root.Items.SetInOrder(item, x => ((ViewerM)x).Name);
      All.Add(item);
      DataAdapter.IsModified = true;

      return item;
    }

    public bool ItemCanRename(string name) =>
      !All.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void ItemRename(ViewerM item, string name) {
      item.Name = name;
      item.Parent.Items.SetInOrder(item, x => ((ViewerM)x).Name);
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(ViewerM viewer) {
      viewer.Parent.Items.Remove(viewer);
      viewer.Parent = null;
      viewer.IncludedFolders.Clear();
      viewer.ExcludedFolders.Clear();
      viewer.ExcludedKeywords.Clear();
      All.Remove(viewer);
      ViewerDeletedEvent(this, new(viewer));
      DataAdapter.IsModified = true;
    }

    public void ToggleCategoryGroup(ViewerM viewer, int groupId) {
      viewer.ExcCatGroupsIds.Toggle(groupId);
      DataAdapter.IsModified = true;
    }

    public void AddFolder(ViewerM viewer, FolderM folder, bool included) {
      (included ? viewer.IncludedFolders : viewer.ExcludedFolders).AddInOrder(folder, (x) => x.FullPath);
      DataAdapter.IsModified = true;
    }

    public void RemoveFolder(ViewerM viewer, FolderM folder, bool included) {
      (included ? viewer.IncludedFolders : viewer.ExcludedFolders).Remove(folder);
      DataAdapter.IsModified = true;
    }

    public void AddKeyword(ViewerM viewer, KeywordM keyword) {
      viewer.ExcludedKeywords.AddInOrder(keyword, (x) => x.FullName);
      DataAdapter.IsModified = true;
    }

    public void RemoveKeyword(ViewerM viewer, KeywordM keyword) {
      viewer.ExcludedKeywords.Remove(keyword);
      DataAdapter.IsModified = true;
    }
  }
}
