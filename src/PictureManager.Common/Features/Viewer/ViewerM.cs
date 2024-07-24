using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.CategoryGroup;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Person;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Common.Features.Viewer;

/// <summary>
/// DB fields: ID|Name|IncludedFolders|ExcludedFolders|ExcludedCategoryGroups|ExcludedKeywords|IsDefault
/// </summary>
public sealed class ViewerM : TreeItem, IEquatable<ViewerM> {
  #region IEquatable implementation
  public bool Equals(ViewerM? other) => Id == other?.Id;
  public override bool Equals(object? obj) => Equals(obj as ViewerM);
  public override int GetHashCode() => Id;
  public static bool operator ==(ViewerM? a, ViewerM? b) {
    if (ReferenceEquals(a, b)) return true;
    if (a is null || b is null) return false;
    return a.Equals(b);
  }
  public static bool operator !=(ViewerM? a, ViewerM? b) => !(a == b);
  #endregion

  public int Id { get; }
  public bool IsDefault { get; set; }
  public ObservableCollection<FolderM> IncludedFolders { get; } = [];
  public ObservableCollection<FolderM> ExcludedFolders { get; } = [];
  public ObservableCollection<KeywordM> ExcludedKeywords { get; } = [];
  public HashSet<CategoryGroupM> ExcludedCategoryGroups { get; } = [];

  private HashSet<FolderM> _incFolders = null!;
  private HashSet<FolderM> _incFoldersTree = null!;
  private HashSet<FolderM> _excFolders = null!;
  private HashSet<KeywordM> _excKeywords = null!;

  public ViewerM(int id, string name, ITreeItem parent) : base(Res.IconEye, name) {
    Id = id;
    Parent = parent;
  }

  public void UpdateHashSets() {
    _incFolders = IncludedFolders.ToHashSet();
    _excFolders = ExcludedFolders.ToHashSet();
    _excKeywords = ExcludedKeywords.ToHashSet();
    _incFoldersTree = [];

    foreach (var fo in IncludedFolders.SelectMany(x => x.GetThisAndParents()))
      _incFoldersTree.Add(fo);
  }

  public bool CanSee(FolderM folder) {
    // If Any part of Test Folder ID matches Any Included Folder ID
    // OR
    // If Any part of Included Folder ID matches Test Folder ID
    var testFos = folder.GetThisAndParents().ToArray();
    var incContain = testFos.Any(testFo => _incFolders.Any(incFo => incFo == testFo))
                     || _incFoldersTree.Any(incFo => incFo == folder);
    var excContain = testFos.Any(testFo => _excFolders.Any(excFo => excFo == testFo));

    return incContain && !excContain;
  }

  public bool CanSeeContentOf(FolderM folder) {
    // If Any part of Test Folder ID matches Any Included Folder ID
    var testFos = folder.GetThisAndParents().ToArray();
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
    if (mi.People?.Any(p => !CanSee(p)) == true) return false;
    if (mi.Keywords?.Any(k => !CanSee(k)) == true) return false;
    if (mi.Segments?.Any(s => s.Person != null && !CanSee(s.Person)) == true) return false;
    if (mi.Segments?.Any(s => s.Keywords?.Any(k => !CanSee(k)) == true) == true) return false;

    return true;
  }

  public bool CanSee(PersonM person) =>
    person.Parent is CategoryGroupM cg && !ExcludedCategoryGroups.Contains(cg);

  public bool CanSee(KeywordM keyword) {
    var keywords = keyword.GetThisAndParents<ITreeItem>().ToArray();
    if (keywords.OfType<CategoryGroupM>().Any(cg => ExcludedCategoryGroups.Contains(cg))) return false;
    if (keywords.OfType<KeywordM>().Any(k => _excKeywords.Contains(k))) return false;

    return true;
  }
}