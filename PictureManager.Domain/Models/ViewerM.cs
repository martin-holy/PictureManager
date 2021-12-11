using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|IncludedFolders|ExcludedFolders|ExcludedCategoryGroups|ExcludedKeywords|IsDefault
  /// </summary>
  public sealed class ViewerM : ObservableObject, IEquatable<ViewerM>, IRecord, ITreeLeaf {
    #region IEquatable implementation
    public bool Equals(ViewerM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as ViewerM);
    public override int GetHashCode() => Id;
    public static bool operator ==(ViewerM a, ViewerM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(ViewerM a, ViewerM b) => !(a == b);
    #endregion

    #region IRecord implementation
    public int Id { get; }
    public string[] Csv { get; set; }
    #endregion

    #region ITreeLeaf implementation
    public ITreeBranch Parent { get; set; }
    #endregion

    private string _name;

    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public bool IsDefault { get; set; }
    public ObservableCollection<FolderM> IncludedFolders { get; } = new();
    public ObservableCollection<FolderM> ExcludedFolders { get; } = new();
    public ObservableCollection<KeywordM> ExcludedKeywords { get; } = new();
    public HashSet<int> ExcCatGroupsIds { get; } = new();

    private readonly HashSet<int> _incFoIds = new();
    private readonly HashSet<int> _incFoTreeIds = new();
    private readonly HashSet<int> _excFoIds = new();
    private readonly HashSet<int> _excKeywordsIds = new();

    public ViewerM(int id, string name, ITreeBranch parent) {
      Id = id;
      Name = name;
      Parent = parent;
    }

    public void UpdateHashSets() {
      _incFoIds.Clear();
      _incFoTreeIds.Clear();
      _excFoIds.Clear();
      _excKeywordsIds.Clear();

      foreach (var folder in IncludedFolders) {
        _incFoIds.Add(folder.Id);
        var fos = new List<FolderM>();
        Tree.GetThisAndParentRecursive(folder, ref fos);
        foreach (var fo in fos)
          _incFoTreeIds.Add(fo.Id);
      }

      foreach (var folder in ExcludedFolders)
        _excFoIds.Add(folder.Id);

      foreach (var keyword in ExcludedKeywords)
        _excKeywordsIds.Add(keyword.Id);
    }

    public bool CanSee(FolderM folder) {
      // If Any part of Test Folder ID matches Any Included Folder ID
      // OR
      // If Any part of Included Folder ID matches Test Folder ID
      var testFos = new List<FolderM>();
      Tree.GetThisAndParentRecursive(folder, ref testFos);
      var incContain = testFos.Any(testFo => _incFoIds.Any(incFoId => incFoId == testFo.Id))
                       || _incFoTreeIds.Any(incFoId => incFoId == folder.Id);
      var excContain = testFos.Any(testFo => _excFoIds.Any(excFoId => excFoId == testFo.Id));

      return incContain && !excContain;
    }

    public bool CanSeeContentOf(FolderM folder) {
      // If Any part of Test Folder ID matches Any Included Folder ID
      var testFos = new List<FolderM>();
      Tree.GetThisAndParentRecursive(folder, ref testFos);
      var incContain = testFos.Any(testFo => _incFoIds.Any(incFoId => incFoId == testFo.Id));
      var excContain = testFos.Any(testFo => _excFoIds.Any(excFoId => excFoId == testFo.Id));

      return incContain && !excContain;
    }

    /// <summary>
    /// Checks for People and Keywords on MediaItem and Segments
    /// </summary>
    /// <param name="mi"></param>
    /// <returns>True if viewer can see MediaItem</returns>
    public bool CanSee(MediaItemM mi) {
      if (mi.People == null && mi.Keywords == null && mi.Segments == null) return true;
      if (mi.People?.Any(p => p.Parent is CategoryGroupM cg && ExcCatGroupsIds.Contains(cg.Id)) == true) return false;
      if (mi.Segments?.Any(s => s.Person?.Parent is CategoryGroupM cg && ExcCatGroupsIds.Contains(cg.Id)) == true) return false;

      var keywords = new List<ITreeLeaf>();
      if (mi.Keywords != null)
        foreach (var keyword in mi.Keywords)
          Tree.GetThisAndParentRecursive(keyword, ref keywords);

      if (mi.Segments != null)
        foreach (var segment in mi.Segments.Where(x => x.Keywords != null))
        foreach (var keyword in segment.Keywords)
          Tree.GetThisAndParentRecursive(keyword, ref keywords);

      if (keywords.OfType<CategoryGroupM>().Any(cg => ExcCatGroupsIds.Contains(cg.Id))) return false;
      if (keywords.OfType<KeywordM>().Any(k => ExcludedKeywords.Contains(k))) return false;

      return true;
    }
  }
}
