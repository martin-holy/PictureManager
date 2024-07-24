using System;
using PictureManager.Common.Features.GeoName;

namespace PictureManager.Common.Features.GeoLocation;

public sealed class GeoLocationM(int id) : IEquatable<GeoLocationM> {
  #region IEquatable implementation
  public bool Equals(GeoLocationM? other) => Id == other?.Id;
  public override bool Equals(object? obj) => Equals(obj as GeoLocationM);
  public override int GetHashCode() => Id;
  public static bool operator ==(GeoLocationM? a, GeoLocationM? b) {
    if (ReferenceEquals(a, b)) return true;
    if (a is null || b is null) return false;
    return a.Equals(b);
  }
  public static bool operator !=(GeoLocationM? a, GeoLocationM? b) => !(a == b);
  #endregion

  public int Id { get; } = id;
  public double? Lat { get; set; }
  public double? Lng { get; set; }
  public GeoNameM? GeoName { get; set; }
}