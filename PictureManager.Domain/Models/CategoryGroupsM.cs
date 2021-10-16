using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class CategoryGroupsM {
    private readonly Core _core;
    public DataAdapter DataAdapter { get; }
    public ObservableCollection<CategoryGroupM> All { get; } = new();
    public Dictionary<Category, ITreeBranch> Categories { get; } = new();

    public CategoryGroupsM(Core core) {
      _core = core;
      DataAdapter = new CategoryGroupsDataAdapter(core, this);
    }

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
      ((ITreeBranch)group.Parent).Items.SetInOrder(group, x => x is CategoryGroupM cg ? cg.Name : string.Empty);
      DataAdapter.IsModified = true;
    }

    public void GroupMove(CategoryGroupM group, CategoryGroupM dest, bool aboveDest) {
      ((ITreeBranch)group.Parent).Items.Move(group, dest, aboveDest);
      DataAdapter.IsModified = true;
    }

    public void GroupDelete(CategoryGroupM group) {
      // move all group items to root
      switch (group.Category) {
        case Category.Keywords:
          foreach (var item in group.Items.Cast<KeywordM>().ToArray())
            _core.KeywordsM.ItemMove(item, _core.KeywordsM, false);
          break;
        case Category.People:
          foreach (var item in group.Items.Cast<PersonM>().ToArray())
            _core.PeopleM.ItemMove(item, _core.PeopleM, false);
          break;
      }

      ((ITreeBranch)group.Parent).Items.Remove(group);
      All.Remove(group);
      DataAdapter.IsModified = true;
    }
  }
}
