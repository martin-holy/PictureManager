using System;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Parent|IsFolderKeyword
  /// </summary>
  public class FoldersDataAdapter : DataAdapter<FolderM> {
    private readonly FoldersM _model;

    public FoldersDataAdapter(SimpleDB.SimpleDB db, FoldersM model) : base("Folders", db) {
      _model = model;
    }

    public override void Load() {
      _model.All.Clear();
      LoadFromFile();
    }

    public override void Save() =>
      SaveToFile(_model.All, ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);

      var folder = string.IsNullOrEmpty(props[2])
        ? new DriveM(int.Parse(props[0]), props[1], null) {
            IsFolderKeyword = props[3] == "1"
          }
        : new FolderM(int.Parse(props[0]), props[1], null) {
            IsFolderKeyword = props[3] == "1"
          };

      _model.All.Add(folder);
      AllCsv.Add(folder, props);
      AllId.Add(folder.Id, folder);
    }

    private static string ToCsv(FolderM folder) =>
      string.Join("|",
        folder.Id.ToString(),
        folder.Name,
        (folder.Parent as FolderM)?.Id.ToString() ?? string.Empty,
        folder.IsFolderKeyword
          ? "1"
          : string.Empty);

    public override void LinkReferences() {
      foreach (var (folder, csv) in AllCsv) {
        // reference to Parent and back reference from Parent to SubFolder
        folder.Parent = !string.IsNullOrEmpty(csv[2])
          ? AllId[int.Parse(csv[2])]
          : _model;
        folder.Parent.Items.Add(folder);
      }
    }
  }
}
