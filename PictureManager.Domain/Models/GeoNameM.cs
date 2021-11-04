using System;
using System.Collections.ObjectModel;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|ToponymName|FCode|Parent
  /// </summary>
  public sealed class GeoNameM :  ObservableObject, IEquatable<GeoNameM>, IRecord, ITreeBranch {
    #region IEquatable implementation
    public bool Equals(GeoNameM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as GeoNameM);
    public override int GetHashCode() => Id;
    public static bool operator ==(GeoNameM a, GeoNameM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(GeoNameM a, GeoNameM b) => !(a == b);
    #endregion

    #region IRecord implementation
    public int Id { get; } // this is GeoNameId not just DB Id
    public string[] Csv { get; set; }
    #endregion

    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    public string Name { get; }
    public string ToponymName { get; }
    public string Fcode { get; }
    public string FullName => Tree.GetFullName(this, "\n", x => x.Name);

    public GeoNameM(int id, string name, string toponymName, string fCode, ITreeBranch parent) {
      Id = id;
      Name = name;
      ToponymName = toponymName;
      Fcode = fCode;
      Parent = parent;
    }
  }
}
