using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using MH.Utils;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.DataAdapters;

namespace PictureManager.Domain.Models {
  public sealed class CategoryGroupsM {
    public CategoryGroupsDataAdapter DataAdapter { get; set; }
    public Dictionary<Category, ITreeItem> Categories { get; } = new();

    public CategoryGroupM GroupCreate(string name, Category category) {
      ITreeItem parent = Categories[category];
      var group = new CategoryGroupM(DataAdapter.GetNextId(), name, category, Res.CategoryToIconName(category)) { Parent = parent };
      group.Items.CollectionChanged += GroupItems_CollectionChanged;
      Tree.SetInOrder(parent.Items, group, x => x.Name);
      DataAdapter.All.Add(group.Id, group);

      return group;
    }

    public void GroupItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      DataAdapter.IsModified = true;
    }

    public static bool ItemCanRename(ITreeItem root, string name) =>
      !root.Items.OfType<CategoryGroupM>().Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void GroupRename(ITreeGroup group, string name) {
      group.Name = name;
      Tree.SetInOrder(group.Parent.Items, group, x => x.Name);
      DataAdapter.IsModified = true;
    }

    public void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest) {
      group.Parent.Items.SetRelativeTo(group, dest, aboveDest);
      DataAdapter.IsModified = true;
    }

    public void GroupDelete(ITreeItem group) {
      // move all group items to root
      if (Tree.GetTopParent(group) is ITreeCategory cat)
        foreach (var item in group.Items.ToArray())
          cat.ItemMove(item, cat, false);

      group.Parent.Items.Remove(group);
      DataAdapter.All.Remove(((CategoryGroupM)group).Id);
      DataAdapter.IsModified = true;
    }

    public void UpdateVisibility(ViewerM viewer) {
      foreach (var (id, group) in DataAdapter.All)
        group.IsHidden = viewer?.ExcCatGroupsIds.Contains(id) == true;
    }
  }
}
