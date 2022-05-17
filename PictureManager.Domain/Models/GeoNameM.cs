using System;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Interfaces;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|ToponymName|FCode|Parent
  /// </summary>
  public sealed class GeoNameM : TreeItem, IEquatable<GeoNameM>, IFilterItem {
    #region IEquatable implementation
    public bool Equals(GeoNameM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as GeoNameM);
    public override int GetHashCode() => Id;
    public static bool operator ==(GeoNameM a, GeoNameM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(GeoNameM a, GeoNameM b) => !(a == b);
    #endregion

    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion

    public int Id { get; } // this is GeoNameId not just DB Id
    public string ToponymName { get; }
    public string Fcode { get; }
    public string FullName => Tree.GetFullName(this, "\n", x => x.Name);

    public GeoNameM(int id, string name, string toponymName, string fCode, ITreeItem parent) : base(Res.IconLocationCheckin, name) {
      Id = id;
      ToponymName = toponymName;
      Fcode = fCode;
      Parent = parent;
    }
  }
}
