using System;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|ToponymName|FCode|Parent
  /// </summary>
  public class GeoNamesDataAdapter : DataAdapter<GeoNameM> {
    private readonly GeoNamesM _model;

    public GeoNamesDataAdapter(SimpleDB.SimpleDB db, GeoNamesM model) : base("GeoNames", db) {
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
      if (props.Length != 5) throw new ArgumentException("Incorrect number of values.", csv);
      var geoName = new GeoNameM(int.Parse(props[0]), props[1], props[2], props[3], null);
      _model.All.Add(geoName);
      AllCsv.Add(geoName, props);
      AllId.Add(geoName.Id, geoName);
    }

    private static string ToCsv(GeoNameM geoName) =>
      string.Join("|",
        geoName.Id.ToString(),
        geoName.Name,
        geoName.ToponymName,
        geoName.Fcode,
        (geoName.Parent as GeoNameM)?.Id.ToString());

    public override void LinkReferences() {
      foreach (var (geoName, csv) in AllCsv) {
        // reference to parent and back reference to children
        geoName.Parent = !string.IsNullOrEmpty(csv[4])
          ? AllId[int.Parse(csv[4])]
          : _model;
        geoName.Parent.Items.Add(geoName);
      }
    }
  }
}
