using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.EventsArgs;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class CategoryGroupsM {
    public DataAdapter DataAdapter { get; set; }
    public ObservableCollection<CategoryGroupM> All { get; } = new();
    public Dictionary<Category, ITreeBranch> Categories { get; } = new();
    public event EventHandler<CategoryGroupDeletedEventArgs> CategoryGroupDeletedEvent = delegate { };

    public CategoryGroupM GroupCreate(string name, Category category) {
      ITreeBranch parent = Categories[category];
      var group = new CategoryGroupM(DataAdapter.GetNextId(), name, category) { Parent = parent };
      group.Items.CollectionChanged += GroupItems_CollectionChanged;
      parent.Items.SetInOrder(group, x => x is CategoryGroupM cg ? cg.Name : string.Empty);
      DataAdapter.IsModified = true;
      return group;
    }

    public void GroupItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      DataAdapter.IsModified = true;
    }

    public static bool ItemCanRename(ITreeBranch root, string name) =>
      !root.Items.OfType<CategoryGroupM>().Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void GroupRename(CategoryGroupM group, string name) {
      group.Name = name;
      group.Parent.Items.SetInOrder(group, x => x is CategoryGroupM cg ? cg.Name : string.Empty);
      DataAdapter.IsModified = true;
    }

    public void GroupMove(CategoryGroupM group, CategoryGroupM dest, bool aboveDest) {
      group.Parent.Items.Move(group, dest, aboveDest);
      DataAdapter.IsModified = true;
    }

    public void GroupDelete(CategoryGroupM group) {
      group.Parent.Items.Remove(group);
      All.Remove(group);
      CategoryGroupDeletedEvent(this, new(group));
      DataAdapter.IsModified = true;
    }

    public void UpdateVisibility(ViewerM viewer) {
      foreach (var group in All)
        group.IsHidden = viewer?.ExcCatGroupsIds.Contains(group.Id) == true;
    }
  }
}
