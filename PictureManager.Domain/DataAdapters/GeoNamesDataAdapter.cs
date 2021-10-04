using PictureManager.Domain.Models;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|ToponymName|FCode|Parent
  /// </summary>
  public class GeoNamesDataAdapter : DataAdapter {
    private readonly GeoNames _model;

    public GeoNamesDataAdapter(Core core, GeoNames model) : base(nameof(GeoNames), core.Sdb) {
      _model = model;
    }

    public override void Load() {
      _model.All.Clear();
      _model.AllDic = new Dictionary<int, GeoName>();
      LoadFromFile();
    }

    public override void Save() => SaveToFile(_model.All.Cast<GeoName>(), ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 5) throw new ArgumentException("Incorrect number of values.", csv);
      var geoName = new GeoName(int.Parse(props[0]), props[1], props[2], props[3], null) { Csv = props };
      _model.All.Add(geoName);
      _model.AllDic.Add(geoName.Id, geoName);
    }

    public static string ToCsv(GeoName geoName) =>
      string.Join("|",
        geoName.Id.ToString(),
        geoName.Title,
        geoName.ToponymName,
        geoName.Fcode,
        (geoName.Parent as GeoName)?.Id.ToString());

    public override void LinkReferences() {
      foreach (var geoName in _model.All.Cast<GeoName>()) {
        // reference to parent and back reference to children
        geoName.Parent = !string.IsNullOrEmpty(geoName.Csv[4]) ? _model.AllDic[int.Parse(geoName.Csv[4])] : _model;
        geoName.Parent.Items.Add(geoName);
        // csv array is not needed any more
        geoName.Csv = null;
      }
    }
  }
}
