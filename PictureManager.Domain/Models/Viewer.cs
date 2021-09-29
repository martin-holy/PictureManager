using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class Viewer : CatTreeViewItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; }
    public bool IsDefault { get; set; }
    public ObservableCollection<Folder> IncludedFolders { get; } = new();
    public ObservableCollection<Folder> ExcludedFolders { get; } = new();
    public HashSet<int> ExcCatGroupsIds { get; } = new();

    private readonly HashSet<int> _incFoIds = new();
    private readonly HashSet<int> _incFoTreeIds = new();
    private readonly HashSet<int> _excFoIds = new();

    public Viewer(int id, string name, ICatTreeViewItem parent) {
      Id = id;
      Title = name;
      Parent = parent;
      IconName = IconName.Eye;
    }

    // ID|Name|IncludedFolders|ExcludedFolders|ExcludedCategoryGroups|IsDefault
    public string ToCsv() =>
      string.Join("|",
        Id.ToString(),
        Title,
        string.Join(",", IncludedFolders.Select(x => x.Id)),
        string.Join(",", ExcludedFolders.Select(x => x.Id)),
        string.Join(",", ExcCatGroupsIds),
        IsDefault ? "1" : string.Empty);

    public void Activate() {
      UpdateHashSets();
      UpdateCategoryGroupsVisibility();
    }

    public void AddFolder(Folder folder, bool included) => SetInPlace(included ? IncludedFolders : ExcludedFolders, folder);

    public void RemoveFolder(Folder folder, bool included) => (included ? IncludedFolders : ExcludedFolders).Remove(folder);

    private static void SetInPlace(ObservableCollection<Folder> collection, Folder item) {
      collection.Add(item);
      var idx = collection.OrderBy(x => x.FullPath).ToList().IndexOf(item);
      collection.Move(collection.IndexOf(item), idx);
    }

    private void UpdateHashSets() {
      _incFoIds.Clear();
      _incFoTreeIds.Clear();
      _excFoIds.Clear();

      foreach (var folder in IncludedFolders) {
        _incFoIds.Add(folder.Id);
        var fos = new List<ICatTreeViewItem>();
        CatTreeViewUtils.GetThisAndParentRecursive(folder, ref fos);
        foreach (var fo in fos.OfType<Folder>())
          _incFoTreeIds.Add(fo.Id);
      }

      foreach (var folder in ExcludedFolders)
        _excFoIds.Add(folder.Id);
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

    public void ToggleCategoryGroup(int groupId) {
      ExcCatGroupsIds.Toggle(groupId);
      Core.Instance.Sdb.SetModified<Viewers>();
    }

    private void UpdateCategoryGroupsVisibility() {
      foreach (var g in Core.Instance.CategoryGroups.All.Cast<CategoryGroup>())
        g.IsHidden = ExcCatGroupsIds.Contains(g.Id);
    }
  }
}
