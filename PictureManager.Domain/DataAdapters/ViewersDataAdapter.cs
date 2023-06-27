using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using System.Linq;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|IncludedFolders|ExcludedFolders|ExcludedCategoryGroups|ExcludedKeywords|IsDefault
  /// </summary>
  public class ViewersDataAdapter : DataAdapter<ViewerM> {
    private readonly ViewersM _model;
    private readonly FoldersM _foldersM;
    private readonly KeywordsM _keywordsM;
    private readonly CategoryGroupsM _categoryGroupsM;

    public ViewersDataAdapter(ViewersM model, FoldersM f, KeywordsM k, CategoryGroupsM g) : base("Viewers", 7) {
      _model = model;
      _foldersM = f;
      _keywordsM = k;
      _categoryGroupsM = g;
    }

    public override ViewerM FromCsv(string[] csv) {
      var viewer = new ViewerM(int.Parse(csv[0]), csv[1], _model) {
        IsDefault = csv[6] == "1"
      };

      if (viewer.IsDefault)
        _model.Current = viewer;

      return viewer;
    }

    public override string ToCsv(ViewerM viewer) =>
      string.Join("|",
        viewer.GetHashCode().ToString(),
        viewer.Name,
        string.Join(",", viewer.IncludedFolders.Select(x => x.GetHashCode().ToString())),
        string.Join(",", viewer.ExcludedFolders.Select(x => x.GetHashCode().ToString())),
        string.Join(",", viewer.ExcludedCategoryGroups.Select(x => x.GetHashCode().ToString())),
        string.Join(",", viewer.ExcludedKeywords.Select(x => x.GetHashCode().ToString())),
        viewer.IsDefault
          ? "1"
          : string.Empty);

    public override void LinkReferences() {
      _model.Items.Clear();

      foreach (var (viewer, csv) in AllCsv.OrderBy(x => x.Item1.Name)) {
        // reference to IncludedFolders
        if (!string.IsNullOrEmpty(csv[2]))
          foreach (var folderId in csv[2].Split(',').Select(int.Parse)) {
            var f = _foldersM.DataAdapter.AllDict.TryGetValue(folderId, out var incF)
              ? incF
              : new(folderId, "?", null);
            viewer.IncludedFolders.SetInOrder(f, x => x.FullPath);
          }

        // reference to ExcludedFolders
        if (!string.IsNullOrEmpty(csv[3]))
          foreach (var folderId in csv[3].Split(',').Select(int.Parse)) {
            var f = _foldersM.DataAdapter.AllDict.TryGetValue(folderId, out var excF)
              ? excF
              : new(folderId, "?", null);
            viewer.ExcludedFolders.SetInOrder(f, x => x.FullPath);
          }

        // ExcludedCategoryGroups
        if (!string.IsNullOrEmpty(csv[4]))
          foreach (var groupId in csv[4].Split(','))
            viewer.ExcludedCategoryGroups.Add(_categoryGroupsM.DataAdapter.AllDict[int.Parse(groupId)]);

        // ExcKeywords
        if (!string.IsNullOrEmpty(csv[5]))
          foreach (var keywordId in csv[5].Split(','))
            viewer.ExcludedKeywords.Add(_keywordsM.DataAdapter.AllDict[int.Parse(keywordId)]);

        // adding Viewer to Viewers
        _model.Items.Add(viewer);
      }

      _model.Current?.UpdateHashSets();

      foreach (var group in _categoryGroupsM.DataAdapter.AllDict.Values)
        group.IsHidden = _model.Current?.ExcludedCategoryGroups.Contains(group) == true;
    }
  }
}
