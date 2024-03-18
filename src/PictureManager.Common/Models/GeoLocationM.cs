using System;

namespace PictureManager.Common.Models;

public sealed class GeoLocationM(int id) : IEquatable<GeoLocationM> {
  #region IEquatable implementation
  public bool Equals(GeoLocationM other) => Id == other?.Id;
  public override bool Equals(object obj) => Equals(obj as GeoLocationM);
  public override int GetHashCode() => Id;
  public static bool operator ==(GeoLocationM a, GeoLocationM b) => a?.Equals(b) ?? b is null;
  public static bool operator !=(GeoLocationM a, GeoLocationM b) => !(a == b);
  #endregion

  public int Id { get; } = id;
  public double? Lat { get; set; }
  public double? Lng { get; set; }
  public GeoNameM GeoName { get; set; }
}