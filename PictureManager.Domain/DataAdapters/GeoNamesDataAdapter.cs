using MH.Utils;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|ToponymName|FCode|Parent
  /// </summary>
  public class GeoNamesDataAdapter : DataAdapter<GeoNameM> {
    private readonly GeoNamesM _model;

    public GeoNamesDataAdapter(GeoNamesM model) : base("GeoNames", 5) {
      _model = model;
    }

    public override GeoNameM FromCsv(string[] csv) =>
      new(int.Parse(csv[0]), csv[1], csv[2], csv[3], null);

    public override string ToCsv(GeoNameM geoName) =>
      string.Join("|",
        geoName.Id.ToString(),
        geoName.Name,
        geoName.ToponymName,
        geoName.Fcode,
        (geoName.Parent as GeoNameM)?.Id.ToString());

    public override void LinkReferences() {
      _model.Items.Clear();

      foreach (var (geoName, csv) in AllCsv) {
        // reference to parent and back reference to children
        geoName.Parent = !string.IsNullOrEmpty(csv[4])
          ? All[int.Parse(csv[4])]
          : _model;
        geoName.Parent.Items.Add(geoName);
      }
    }
  }
}
