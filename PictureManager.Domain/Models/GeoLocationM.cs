using System;

namespace PictureManager.Domain.Models;

public sealed class GeoLocationM : IEquatable<GeoLocationM> {
  #region IEquatable implementation
  public bool Equals(GeoLocationM other) => Id == other?.Id;
  public override bool Equals(object obj) => Equals(obj as GeoLocationM);
  public override int GetHashCode() => Id;
  public static bool operator ==(GeoLocationM a, GeoLocationM b) => a?.Equals(b) ?? b is null;
  public static bool operator !=(GeoLocationM a, GeoLocationM b) => !(a == b);
  #endregion

  public int Id { get; }
  public double? Lat { get; set; }
  public double? Lng { get; set; }
  public GeoNameM GeoName { get; set; }

  public GeoLocationM(int id) {
    Id = id;
  }
}