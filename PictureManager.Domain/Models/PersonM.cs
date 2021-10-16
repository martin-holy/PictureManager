using System;
using SimpleDB;
using System.Collections.Generic;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Interfaces;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|Segments|Keywords
  /// </summary>
  public sealed class PersonM : ObservableObject, IEquatable<PersonM>, IRecord, ITreeLeaf {
    #region IEquatable implementation
    public bool Equals(PersonM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as PersonM);
    public override int GetHashCode() => Id;
    public static bool operator ==(PersonM a, PersonM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(PersonM a, PersonM b) => !(a == b);
    #endregion

    #region IRecord implementation
    public int Id { get; }
    public string[] Csv { get; set; }
    #endregion

    #region ITreeLeaf implementation
    public object Parent { get; set; }
    #endregion

    private string _name;
    private Segment _segment;

    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public Segment Segment { get => _segment; set { _segment = value; OnPropertyChanged(); } }
    public List<Segment> Segments { get; set; } // Top Segments only
    public List<KeywordM> Keywords { get; set; }

    public PersonM() { }

    public PersonM(int id, string name) {
      Id = id;
      Name = name;
    }
  }
}
