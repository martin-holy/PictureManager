using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|IncludedFolders|ExcludedFolders|ExcludedCategoryGroups|ExcludedKeywords|IsDefault
  /// </summary>
  public sealed class ViewerM : TreeItem, IEquatable<ViewerM> {
    #region IEquatable implementation
    public bool Equals(ViewerM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as ViewerM);
    public override int GetHashCode() => Id;
    public static bool operator ==(ViewerM a, ViewerM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(ViewerM a, ViewerM b) => !(a == b);
    #endregion

    public int Id { get; }
    public bool IsDefault { get; set; }
    public ObservableCollection<FolderM> IncludedFolders { get; } = new();
    public ObservableCollection<FolderM> ExcludedFolders { get; } = new();
    public ObservableCollection<KeywordM> ExcludedKeywords { get; } = new();
    public HashSet<CategoryGroupM> ExcludedCategoryGroups { get; } = new();
    public ObservableCollection<ListItem<CategoryGroupM>> CategoryGroups { get; } = new();

    private HashSet<FolderM> _incFolders;
    private HashSet<FolderM> _incFoldersTree;
    private HashSet<FolderM> _excFolders;
    private HashSet<KeywordM> _excKeywords;

    public ViewerM(int id, string name, ITreeItem parent) : base(Res.IconEye, name) {
      Id = id;
      Parent = parent;
    }

    public void UpdateExcludedCategoryGroups() {
      ExcludedCategoryGroups.Clear();
      foreach (var cg in CategoryGroups.Where(x => !x.IsSelected))
        ExcludedCategoryGroups.Add(cg.Content);
    }

    public void Reload(IEnumerable<CategoryGroupM> categoryGroups) {
      ReloadCategoryGroups(categoryGroups);

      foreach (var licg in CategoryGroups)
        licg.IsSelected = !ExcludedCategoryGroups.Contains(licg.Content);
    }

    private void ReloadCategoryGroups(IEnumerable<CategoryGroupM> categoryGroups) {
      var groups = categoryGroups
        .OrderBy(x => x.Category)
        .ThenBy(x => x.Name);

      CategoryGroups.Clear();

      foreach (var cg in groups)
        CategoryGroups.Add(new(cg));
    }

    public void UpdateHashSets() {
      _incFolders = IncludedFolders.ToHashSet();
      _excFolders = ExcludedFolders.ToHashSet();
      _excKeywords = ExcludedKeywords.ToHashSet();
      _incFoldersTree = new();

      foreach (var folder in IncludedFolders) {
        var fos = new List<FolderM>();
        Tree.GetThisAndParentRecursive(folder, ref fos);
        foreach (var fo in fos)
          _incFoldersTree.Add(fo);
      }
    }

    public bool CanSee(FolderM folder) {
      // If Any part of Test Folder ID matches Any Included Folder ID
      // OR
      // If Any part of Included Folder ID matches Test Folder ID
      var testFos = new List<FolderM>();
      Tree.GetThisAndParentRecursive(folder, ref testFos);
      var incContain = testFos.Any(testFo => _incFolders.Any(incFo => incFo == testFo))
                       || _incFoldersTree.Any(incFo => incFo == folder);
      var excContain = testFos.Any(testFo => _excFolders.Any(excFo => excFo == testFo));

      return incContain && !excContain;
    }

    public bool CanSeeContentOf(FolderM folder) {
      // If Any part of Test Folder ID matches Any Included Folder ID
      var testFos = new List<FolderM>();
      Tree.GetThisAndParentRecursive(folder, ref testFos);
      var incContain = testFos.Any(testFo => _incFolders.Any(incFo => incFo == testFo));
      var excContain = testFos.Any(testFo => _excFolders.Any(excFo => excFo == testFo));

      return incContain && !excContain;
    }

    /// <summary>
    /// Checks for People and Keywords on MediaItem and Segments
    /// </summary>
    /// <param name="mi"></param>
    /// <returns>True if viewer can see MediaItem</returns>
    public bool CanSee(MediaItemM mi) {
      if (mi.People == null && mi.Keywords == null && mi.Segments == null) return true;
      if (mi.People?.Any(p => p.Parent is CategoryGroupM cg && ExcludedCategoryGroups.Contains(cg)) == true) return false;
      if (mi.Segments?.Any(s => s.Person?.Parent is CategoryGroupM cg && ExcludedCategoryGroups.Contains(cg)) == true) return false;

      var keywords = new List<ITreeItem>();
      if (mi.Keywords != null)
        foreach (var keyword in mi.Keywords)
          Tree.GetThisAndParentRecursive(keyword, ref keywords);

      if (mi.Segments != null)
        foreach (var segment in mi.Segments.Where(x => x.Keywords != null))
        foreach (var keyword in segment.Keywords)
          Tree.GetThisAndParentRecursive(keyword, ref keywords);

      if (keywords.OfType<CategoryGroupM>().Any(cg => ExcludedCategoryGroups.Contains(cg))) return false;
      if (keywords.OfType<KeywordM>().Any(k => _excKeywords.Contains(k))) return false;

      return true;
    }
  }
}
