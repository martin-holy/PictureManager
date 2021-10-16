using PictureManager.Domain.Models;
using SimpleDB;
using System;
using System.Linq;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|IncludedFolders|ExcludedFolders|ExcludedCategoryGroups|ExcludedKeywords|IsDefault
  /// </summary>
  public class ViewersDataAdapter : DataAdapter {
    private readonly Core _core;
    private readonly Viewers _model;

    public ViewersDataAdapter(Core core, Viewers model) : base("Viewers", core.Sdb) {
      _core = core;
      _model = model;
    }

    public override void Load() {
      _model.All.Clear();
      LoadFromFile();
    }

    public override void Save() => SaveToFile(_model.All.Cast<Viewer>(), ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 7) throw new ArgumentException("Incorrect number of values.", csv);
      var viewer = new Viewer(int.Parse(props[0]), props[1], _model) { Csv = props, IsDefault = props[6] == "1" };
      if (viewer.IsDefault) _core.CurrentViewer = viewer;
      _model.All.Add(viewer);
    }

    public static string ToCsv(Viewer viewer) =>
      string.Join("|",
        viewer.Id.ToString(),
        viewer.Title,
        string.Join(",", viewer.IncludedFolders.Select(x => x.Id)),
        string.Join(",", viewer.ExcludedFolders.Select(x => x.Id)),
        string.Join(",", viewer.ExcCatGroupsIds),
        string.Join(",", viewer.ExcludedKeywords.Select(x => x.Id)),
        viewer.IsDefault ? "1" : string.Empty);

    public override void LinkReferences() {
      _model.Items.Clear();

      foreach (var viewer in _model.All.Cast<Viewer>().OrderBy(x => x.Title)) {
        // reference to IncludedFolders
        if (!string.IsNullOrEmpty(viewer.Csv[2]))
          foreach (var folderId in viewer.Csv[2].Split(',')) {
            var f = _core.Folders.AllDic[int.Parse(folderId)];
            viewer.AddFolder(f, true);
          }

        // reference to ExcludedFolders
        if (!string.IsNullOrEmpty(viewer.Csv[3]))
          foreach (var folderId in viewer.Csv[3].Split(',')) {
            var f = _core.Folders.AllDic[int.Parse(folderId)];
            viewer.AddFolder(f, false);
          }

        // ExcludedCategoryGroups
        if (!string.IsNullOrEmpty(viewer.Csv[4]))
          foreach (var groupId in viewer.Csv[4].Split(','))
            viewer.ExcCatGroupsIds.Add(int.Parse(groupId));

        // ExcKeywords
        if (!string.IsNullOrEmpty(viewer.Csv[5]))
          foreach (var keywordId in viewer.Csv[5].Split(','))
            viewer.ExcludedKeywords.Add(_core.KeywordsM.AllDic[int.Parse(keywordId)]);

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
