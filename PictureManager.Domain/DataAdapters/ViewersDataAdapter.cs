using System;
using System.Linq;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|IncludedFolders|ExcludedFolders|ExcludedCategoryGroups|ExcludedKeywords|IsDefault
  /// </summary>
  public class ViewersDataAdapter : DataAdapter {
    private readonly ViewersM _model;
    private readonly FoldersM _foldersM;
    private readonly KeywordsM _keywordsM;

    public ViewersDataAdapter(SimpleDB.SimpleDB db, ViewersM model, FoldersM f, KeywordsM k) : base("Viewers", db) {
      _model = model;
      _foldersM = f;
      _keywordsM = k;
    }

    public override void Load() {
      _model.All.Clear();
      LoadFromFile();
    }

    public override void Save() =>
      SaveToFile(_model.All, ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 7) throw new ArgumentException("Incorrect number of values.", csv);
      var viewer = new ViewerM(int.Parse(props[0]), props[1], _model) {
        Csv = props,
        IsDefault = props[6] == "1"
      };
      if (viewer.IsDefault)
        _model.Current = viewer;
      _model.All.Add(viewer);
    }

    public static string ToCsv(ViewerM viewer) =>
      string.Join("|",
        viewer.Id.ToString(),
        viewer.Name,
        string.Join(",", viewer.IncludedFolders.Select(x => x.Id)),
        string.Join(",", viewer.ExcludedFolders.Select(x => x.Id)),
        string.Join(",", viewer.ExcCatGroupsIds),
        string.Join(",", viewer.ExcludedKeywords.Select(x => x.Id)),
        viewer.IsDefault
          ? "1"
          : string.Empty);

    public override void LinkReferences() {
      _model.Items.Clear();

      foreach (var viewer in _model.All.OrderBy(x => x.Name)) {
        // reference to IncludedFolders
        if (!string.IsNullOrEmpty(viewer.Csv[2]))
          foreach (var folderId in viewer.Csv[2].Split(',')) {
            var f = _foldersM.AllDic[int.Parse(folderId)];
            viewer.IncludedFolders.SetInOrder(f, x => x.FullPath);
          }

        // reference to ExcludedFolders
        if (!string.IsNullOrEmpty(viewer.Csv[3]))
          foreach (var folderId in viewer.Csv[3].Split(',')) {
            var f = _foldersM.AllDic[int.Parse(folderId)];
            viewer.ExcludedFolders.SetInOrder(f, x => x.FullPath);
          }

        // ExcludedCategoryGroups
        if (!string.IsNullOrEmpty(viewer.Csv[4]))
          foreach (var groupId in viewer.Csv[4].Split(','))
            viewer.ExcCatGroupsIds.Add(int.Parse(groupId));

        // ExcKeywords
        if (!string.IsNullOrEmpty(viewer.Csv[5]))
          foreach (var keywordId in viewer.Csv[5].Split(','))
            viewer.ExcludedKeywords.Add(_keywordsM.AllDic[int.Parse(keywordId)]);

        if (viewer.IsDefault)
          viewer.UpdateHashSets();

        // adding Viewer to Viewers
        _model.Items.Add(viewer);

        // CSV array is not needed any more
        viewer.Csv = null;
      }
    }
  }
}
