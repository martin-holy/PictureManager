using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.TreeCategories;
using System.Linq;

namespace PictureManager.Domain.Repositories;

/// <summary>
/// DB fields: ID|Name|IncludedFolders|ExcludedFolders|ExcludedCategoryGroups|ExcludedKeywords|IsDefault
/// </summary>
public class ViewerR : TreeDataAdapter<ViewerM> {
  private readonly CoreR _coreR;

  public ViewersTreeCategory Tree { get; }

  public ViewerR(CoreR coreR) : base("Viewers", 7) {
    _coreR = coreR;
    Tree = new(this);
  }

  public override ViewerM FromCsv(string[] csv) {
    var viewer = new ViewerM(int.Parse(csv[0]), csv[1], Tree) {
      IsDefault = csv[6] == "1"
    };

    return viewer;
  }

  public override string ToCsv(ViewerM viewer) =>
    string.Join("|",
      viewer.GetHashCode().ToString(),
      viewer.Name,
      viewer.IncludedFolders.ToHashCodes().ToCsv(),
      viewer.ExcludedFolders.ToHashCodes().ToCsv(),
      viewer.ExcludedCategoryGroups.ToHashCodes().ToCsv(),
      viewer.ExcludedKeywords.ToHashCodes().ToCsv(),
      viewer.IsDefault
        ? "1"
        : string.Empty);

  public override void LinkReferences() {
    Tree.Items.Clear();

    foreach (var (viewer, csv) in AllCsv.OrderBy(x => x.Item1.Name)) {
      // reference to IncludedFolders
      if (!string.IsNullOrEmpty(csv[2]))
        foreach (var folderId in csv[2].Split(',').Select(int.Parse)) {
          var f = _coreR.Folder.AllDict.TryGetValue(folderId, out var incF)
            ? incF
            : new(folderId, "?", null);
          viewer.IncludedFolders.SetInOrder(f, x => x.FullPath);
        }

      // reference to ExcludedFolders
      if (!string.IsNullOrEmpty(csv[3]))
        foreach (var folderId in csv[3].Split(',').Select(int.Parse)) {
          var f = _coreR.Folder.AllDict.TryGetValue(folderId, out var excF)
            ? excF
            : new(folderId, "?", null);
          viewer.ExcludedFolders.SetInOrder(f, x => x.FullPath);
        }

      // ExcludedCategoryGroups
      if (!string.IsNullOrEmpty(csv[4]))
        foreach (var groupId in csv[4].Split(','))
          viewer.ExcludedCategoryGroups.Add(_coreR.CategoryGroup.AllDict[int.Parse(groupId)]);

      // ExcKeywords
      if (!string.IsNullOrEmpty(csv[5]))
        foreach (var keywordId in csv[5].Split(','))
          viewer.ExcludedKeywords.Add(_coreR.Keyword.AllDict[int.Parse(keywordId)]);

      // adding Viewer to Viewers
      Tree.Items.Add(viewer);
    }
  }

  public override ViewerM ItemCreate(ITreeItem parent, string name) =>
    TreeItemCreate(new(GetNextId(), name, parent));

  protected override void OnItemDeleted(ViewerM item) {
    item.Parent.Items.Remove(item);
    item.Parent = null;
    item.IncludedFolders.Clear();
    item.ExcludedFolders.Clear();
    item.ExcludedKeywords.Clear();
  }

  public void AddFolder(ViewerM viewer, FolderM folder, bool included) {
    (included ? viewer.IncludedFolders : viewer.ExcludedFolders).SetInOrder(folder, x => x.FullPath);
    IsModified = true;
  }

  public void RemoveFolder(ViewerM viewer, FolderM folder, bool included) {
    (included ? viewer.IncludedFolders : viewer.ExcludedFolders).Remove(folder);
    IsModified = true;
  }

  public void AddKeyword(ViewerM viewer, KeywordM keyword) {
    viewer.ExcludedKeywords.SetInOrder(keyword, x => x.FullName);
    IsModified = true;
  }

  public void RemoveKeyword(ViewerM viewer, KeywordM keyword) {
    viewer.ExcludedKeywords.Remove(keyword);
    IsModified = true;
  }
}