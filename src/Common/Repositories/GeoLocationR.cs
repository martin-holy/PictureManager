using MH.Utils.BaseClasses;
using PictureManager.Common.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common.Repositories;

/// <summary>
/// DB fields: ID|Lat|Lng|GeoName
/// </summary>
public sealed class GeoLocationR(CoreR coreR) : TableDataAdapter<GeoLocationM>("GeoLocations", 4) {
  public override GeoLocationM FromCsv(string[] csv) =>
    new(int.Parse(csv[0])) {
      Lat = ToDouble(csv[1]),
      Lng = ToDouble(csv[2])
    };

  public override string ToCsv(GeoLocationM gl) =>
    string.Join("|",
      gl.GetHashCode().ToString(),
      ToString(gl.Lat),
      ToString(gl.Lng),
      gl.GeoName?.GetHashCode().ToString());

  public override void LinkReferences() {
    foreach (var (gl, csv) in AllCsv)
      gl.GeoName = coreR.GeoName.GetById(csv[3], true);
  }

  public GeoLocationM ItemCreate(double? lat, double? lng, GeoNameM g) =>
    ItemCreate(new(GetNextId()) {
      Lat = lat,
      Lng = lng,
      GeoName = g
    });

  public async Task<GeoLocationM> GetOrCreate(double? lat, double? lng, int? gnId, GeoNameM gn, bool online = true) {
    if (lat == null && lng == null && gnId == null && gn == null) return null;

    if (gnId != null) {
      gn = coreR.GeoName.All.SingleOrDefault(x => x.GetHashCode() == gnId);
      if (gn == null && online)
        gn = await coreR.GeoName.CreateGeoNameHierarchy((int)gnId);
    }

    GeoLocationM gl;
    if (lat != null && lng != null) {
      gl = All.SingleOrDefault(x =>
        x.Lat != null &&
        x.Lng != null &&
        Math.Abs((double)x.Lat - (double)lat) < 0.00001 &&
        Math.Abs((double)x.Lng - (double)lng) < 0.00001);

      if (gl?.GeoName != null) gn = gl.GeoName;

      if (gn == null && online)
        gn = await coreR.GeoName.CreateGeoNameHierarchy((double)lat, (double)lng);

      if (gl != null && gl.GeoName == null && gn != null) {
        gl.GeoName = gn;
        RaiseItemUpdated(gl);
        IsModified = true;
      }
    }
    else {
      if (gn == null) return null;
      gl = All.SingleOrDefault(x => ReferenceEquals(x.GeoName, gn) && x.Lat == null && x.Lng == null);
    }

    return gl ?? ItemCreate(
      lat == null ? null : Math.Round((double)lat, 5),
      lng == null ? null : Math.Round((double)lng, 5),
      gn);
  }

  public void RemoveGeoName(GeoNameM geoName) {
    var all = All.Where(x => ReferenceEquals(x.GeoName, geoName)).ToArray();
    var toDelete = all.Where(x => x.Lat == null && x.Lng == null).ToArray();

    foreach (var gl in all.Except(toDelete)) {
      gl.GeoName = null;
      RaiseItemUpdated(gl);
    }

    ItemsDelete(toDelete);
    IsModified = true;
  }
}