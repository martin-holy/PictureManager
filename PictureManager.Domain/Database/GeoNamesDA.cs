using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|Name|ToponymName|FCode|Parent
/// </summary>
public class GeoNamesDA : TreeDataAdapter<GeoNameM> {
  public GeoNamesM Model { get; }

  public GeoNamesDA() : base("GeoNames", 5) {
    Model = new(this);
  }

  public override GeoNameM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1], csv[2], csv[3], null);

  public override string ToCsv(GeoNameM geoName) =>
    string.Join("|",
      geoName.GetHashCode().ToString(),
      geoName.Name,
      geoName.ToponymName,
      geoName.Fcode,
      (geoName.Parent as GeoNameM)?.GetHashCode().ToString());

  public override void LinkReferences() {
    Model.TreeCategory.Items.Clear();
    LinkTree(Model.TreeCategory, 4);
  }

  public GeoNameM ItemCreate(int id, string name, string toponymName, string fCode, ITreeItem parent) =>
    TreeItemCreate(new(id, name, toponymName, fCode, parent));
}