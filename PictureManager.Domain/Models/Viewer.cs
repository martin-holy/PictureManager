﻿using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class Viewer : CatTreeViewItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; }
    public bool IsDefault { get; set; }

    public CatTreeViewItem IncludedFolders { get; }
    public CatTreeViewItem ExcludedFolders { get; }
    public CatTreeViewItem CategoryGroups { get; }
    public HashSet<int> ExcCatGroupsIds { get; }

    private readonly HashSet<int> _incFoIds;
    private readonly HashSet<int> _incFoTreeIds;
    private readonly HashSet<int> _excFoIds;

    public Viewer(int id, string name, ICatTreeViewItem parent) {
      Id = id;
      Title = name;
      Parent = parent;

      IncludedFolders = new() { Title = "Included Folders", IconName = IconName.FolderStar, Parent = this };
      ExcludedFolders = new() { Title = "Excluded Folders", IconName = IconName.FolderStar, Parent = this };
      CategoryGroups = new() { Title = "Category Groups", IconName = IconName.Group, Parent = this };
      CategoryGroups.Items.Add(new CatTreeViewItem());
      CategoryGroups.OnExpand += (o, e) => ReloadCategoryGroups();

      Items.Add(IncludedFolders);
      Items.Add(ExcludedFolders);
      Items.Add(CategoryGroups);

      IconName = IconName.Eye;

      _incFoIds = new();
      _incFoTreeIds = new();
      _excFoIds = new();
      ExcCatGroupsIds = new();
    }

    // ID|Name|IncludedFolders|ExcludedFolders|ExcludedCategoryGroups|IsDefault
    public string ToCsv() =>
      string.Join("|",
        Id.ToString(),
        Title,
        string.Join(",", IncludedFolders.Items.Select(x => x.Tag)),
        string.Join(",", ExcludedFolders.Items.Select(x => x.Tag)),
        string.Join(",", ExcCatGroupsIds),
        IsDefault ? "1" : string.Empty);

    public void AddFolder(Folder folder, bool included) {
      var item = new CatTreeViewItem {
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
        var fos = new List<ICatTreeViewItem>();
        CatTreeViewUtils.GetThisAndParentRecursive(folder, ref fos);
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
      var testFos = new List<ICatTreeViewItem>();
      CatTreeViewUtils.GetThisAndParentRecursive(folder, ref testFos);
      var incContain = testFos.OfType<Folder>().Any(testFo => _incFoIds.Any(incFoId => incFoId == testFo.Id))
                       || _incFoTreeIds.Any(incFoId => incFoId == folder.Id);
      var excContain = testFos.OfType<Folder>().Any(testFo => _excFoIds.Any(excFoId => excFoId == testFo.Id));

      return incContain && !excContain;
    }

    public bool CanSeeContentOfThisFolder(Folder folder) {
      // If Any part of Test Folder ID matches Any Included Folder ID
      var testFos = new List<ICatTreeViewItem>();
      CatTreeViewUtils.GetThisAndParentRecursive(folder, ref testFos);
      var incContain = testFos.OfType<Folder>().Any(testFo => _incFoIds.Any(incFoId => incFoId == testFo.Id));
      var excContain = testFos.OfType<Folder>().Any(testFo => _excFoIds.Any(excFoId => excFoId == testFo.Id));

      return incContain && !excContain;
    }

    public void ReloadCategoryGroups() {
      CategoryGroups.Items.Clear();
      foreach (var g in Core.Instance.CategoryGroups.All.Cast<CategoryGroup>().OrderBy(x => x.Category).ThenBy(x => x.Title)) {
        CategoryGroups.Items.Add(new CatTreeViewItem() {
          Tag = g.Id,
          Title = g.Title,
          IconName = g.IconName,
          Parent = CategoryGroups,
          IsMarked = !ExcCatGroupsIds.Contains(g.Id)
        });
      }
    }

    public void ToggleCategoryGroup(ICatTreeViewItem group) {
      ExcCatGroupsIds.Toggle((int)group.Tag);
      group.IsMarked = !group.IsMarked;
      Core.Instance.Sdb.SetModified<Viewers>();
    }

    public void UpdateCategoryGroupsVisibility() {
      foreach (var g in Core.Instance.CategoryGroups.All.Cast<CategoryGroup>())
        g.IsHidden = ExcCatGroupsIds.Contains(g.Id);
    }

    private static void SetInPlace(ICatTreeViewItem item) {
      item.Parent.Items.Add(item);
      var idx = item.Parent.Items.OrderBy(x => x.ToolTip).ToList().IndexOf(item);
      item.Parent.Items.Move(item.Parent.Items.IndexOf(item), idx);
    }
  }
}
