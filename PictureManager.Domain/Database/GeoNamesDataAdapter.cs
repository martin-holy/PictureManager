using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|Name|ToponymName|FCode|Parent
/// </summary>
public class GeoNamesDataAdapter : TreeDataAdapter<GeoNameM> {
  public GeoNamesM Model { get; }

  public GeoNamesDataAdapter() : base("GeoNames", 5) {
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

    foreach (var (geoName, csv) in AllCsv) {
      // reference to parent and back reference to children
      geoName.Parent = !string.IsNullOrEmpty(csv[4])
        ? AllDict[int.Parse(csv[4])]
        : Model.TreeCategory;
      geoName.Parent.Items.Add(geoName);
    }
  }
}