using System;
using System.Linq;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Segments|Keywords
  /// </summary>
  public class PeopleDataAdapter : DataAdapter {
    private readonly Core _core;
    private readonly PeopleM _model;

    public PeopleDataAdapter(Core core, PeopleM model) : base("People", core.Sdb) {
      _core = core;
      _model = model;
    }

    public override void Load() {
      _model.All.Clear();
      _model.AllDic = new();
      LoadFromFile();
    }

    public override void Save() => SaveToFile(_model.GetAll(), ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);
      var person = new PersonM(int.Parse(props[0]), props[1]) { Csv = props };
      _model.All.Add(person);
      _model.AllDic.Add(person.Id, person);
    }

    private static string ToCsv(PersonM person) =>
      string.Join("|",
        person.Id.ToString(),
        person.Name,
        person.Segments == null ? string.Empty : string.Join(",", person.Segments.Select(x => x.Id)),
        person.Keywords == null ? string.Empty : string.Join(",", person.Keywords.Select(x => x.Id)));

    public override void LinkReferences() {
      foreach (var person in _model.All) {
        // Persons top segments
        if (!string.IsNullOrEmpty(person.Csv[2])) {
          var ids = person.Csv[2].Split(',');
          person.Segments = new(ids.Length);
          foreach (var segmentId in ids)
            person.Segments.Add(_core.SegmentsM.AllDic[int.Parse(segmentId)]);
          person.Segment = person.Segments[0];
        }

        // reference to Keywords
        if (!string.IsNullOrEmpty(person.Csv[3])) {
          var ids = person.Csv[3].Split(',');
          person.Keywords = new(ids.Length);
          foreach (var keywordId in ids)
            person.Keywords.Add(_core.KeywordsM.AllDic[int.Parse(keywordId)]);
        }

        // add loose people
        foreach (var personM in _model.All.Where(x => x.Parent == null)) {
          personM.Parent = _model;
          _model.Items.Add(personM);
        }

        // CSV array is not needed any more
        person.Csv = null;
      }
    }
  }
}
