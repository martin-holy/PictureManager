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
    public ObservableCollection<Keyword> ExcludedKeywords { get; } = new();
    public HashSet<int> ExcCatGroupsIds { get; } = new();

    private readonly HashSet<int> _incFoIds = new();
    private readonly HashSet<int> _incFoTreeIds = new();
    private readonly HashSet<int> _excFoIds = new();
    private readonly HashSet<int> _excKeywordsIds = new();

    public Viewer(int id, string name, ICatTreeViewItem parent) {
      Id = id;
      Title = name;
      Parent = parent;
      IconName = IconName.Eye;
    }

    public void Activate() {
      UpdateHashSets();
      UpdateCategoryGroupsVisibility();
    }

    public void AddFolder(Folder folder, bool included) => (included ? IncludedFolders : ExcludedFolders).AddInOrder(folder, (x) => x.FullPath);

    public void RemoveFolder(Folder folder, bool included) => (included ? IncludedFolders : ExcludedFolders).Remove(folder);

    public void AddKeyword(Keyword keyword) => ExcludedKeywords.AddInOrder(keyword, (x) => x.FullPath);

    public void RemoveKeyword(Keyword keyword) => ExcludedKeywords.Remove(keyword);

    private void UpdateHashSets() {
      _incFoIds.Clear();
      _incFoTreeIds.Clear();
      _excFoIds.Clear();
      _excKeywordsIds.Clear();

      foreach (var folder in IncludedFolders) {
        _incFoIds.Add(folder.Id);
        var fos = new List<ICatTreeViewItem>();
        CatTreeViewUtils.GetThisAndParentRecursive(folder, ref fos);
        foreach (var fo in fos.OfType<Folder>())
          _incFoTreeIds.Add(fo.Id);
      }

      foreach (var folder in ExcludedFolders)
        _excFoIds.Add(folder.Id);

      foreach (var keyword in ExcludedKeywords)
        _excKeywordsIds.Add(keyword.Id);
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

    /// <summary>
    /// Checks for People and Keywords on MediaItem and Segments
    /// </summary>
    /// <param name="mi"></param>
    /// <returns>True if viewer can see MediaItem</returns>
    public bool CanSee(MediaItem mi) {
      if (mi.People == null && mi.Keywords == null && mi.Segments == null) return true;
      if (mi.People?.Any(p => p.Parent is CategoryGroup cg && ExcCatGroupsIds.Contains(cg.Id)) == true) return false;
      if (mi.Segments?.Any(s => s.Person?.Parent is CategoryGroup cg && ExcCatGroupsIds.Contains(cg.Id)) == true) return false;

      var keywords = new List<ICatTreeViewItem>();
      if (mi.Keywords != null)
        foreach (var keyword in mi.Keywords)
          CatTreeViewUtils.GetThisAndParentRecursive(keyword, ref keywords);

      if (mi.Segments != null)
        foreach (var segment in mi.Segments.Where(x => x.Keywords != null))
          foreach (var keyword in segment.Keywords)
            CatTreeViewUtils.GetThisAndParentRecursive(keyword, ref keywords);

      if (keywords.OfType<CategoryGroup>().Any(cg => ExcCatGroupsIds.Contains(cg.Id))) return false;
      if (keywords.OfType<Keyword>().Any(k => ExcludedKeywords.Contains(k))) return false;

      return true;
    }

    public void ToggleCategoryGroup(int groupId) {
      ExcCatGroupsIds.Toggle(groupId);
      Core.Instance.Viewers.DataAdapter.IsModified = true;
    }

    private void UpdateCategoryGroupsVisibility() {
      foreach (var g in Core.Instance.CategoryGroups.All.Cast<CategoryGroup>())
        g.IsHidden = ExcCatGroupsIds.Contains(g.Id);
    }
  }
}
