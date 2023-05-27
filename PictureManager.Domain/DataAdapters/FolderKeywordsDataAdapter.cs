using MH.Utils;
using PictureManager.Domain.Models;
using System.Linq;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID
  /// </summary>
  public class FolderKeywordsDataAdapter : DataAdapter<FolderM> {
    private readonly FoldersM _foldersM;

    public FolderKeywordsDataAdapter(FoldersM f) : base("FolderKeywords", 1) {
      _foldersM = f;
    }

    public override void Save() =>
      SaveDriveRelated(All
        .GroupBy(x => Tree.GetTopParent(x).Name)
        .ToDictionary(x => x.Key, x => x.Select(y => y)));

    public override FolderM FromCsv(string[] csv) =>
      new(int.Parse(csv[0]), string.Empty, null);

    public override string ToCsv(FolderM folder) =>
      folder.GetHashCode().ToString();

    public override void LinkReferences() {
      foreach (var id in AllDict.Keys)
        AllDict[id] = _foldersM.DataAdapter.AllDict[id];
    }
  }
}
