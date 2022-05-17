using System;
using System.Linq;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|IncludedFolders|ExcludedFolders|ExcludedCategoryGroups|ExcludedKeywords|IsDefault
  /// </summary>
  public class ViewersDataAdapter : DataAdapter<ViewerM> {
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
        IsDefault = props[6] == "1"
      };
      if (viewer.IsDefault)
        _model.Current = viewer;
      _model.All.Add(viewer);
      AllCsv.Add(viewer, props);
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

      foreach (var (viewer, csv) in AllCsv.OrderBy(x => x.Key.Name)) {
        // reference to IncludedFolders
        if (!string.IsNullOrEmpty(csv[2]))
          foreach (var folderId in csv[2].Split(',')) {
            var f = _foldersM.DataAdapter.AllId[int.Parse(folderId)];
            viewer.IncludedFolders.SetInOrder(f, x => x.FullPath);
          }

        // reference to ExcludedFolders
        if (!string.IsNullOrEmpty(csv[3]))
          foreach (var folderId in csv[3].Split(',')) {
            var f = _foldersM.DataAdapter.AllId[int.Parse(folderId)];
            viewer.ExcludedFolders.SetInOrder(f, x => x.FullPath);
          }

        // ExcludedCategoryGroups
        if (!string.IsNullOrEmpty(csv[4]))
          foreach (var groupId in csv[4].Split(','))
            viewer.ExcCatGroupsIds.Add(int.Parse(groupId));

        // ExcKeywords
        if (!string.IsNullOrEmpty(csv[5]))
          foreach (var keywordId in csv[5].Split(','))
            viewer.ExcludedKeywords.Add(_keywordsM.DataAdapter.AllId[int.Parse(keywordId)]);

        // adding Viewer to Viewers
        _model.Items.Add(viewer);
      }
    }
  }
}
