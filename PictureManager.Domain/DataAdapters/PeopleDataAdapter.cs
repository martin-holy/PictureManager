using System;
using System.Linq;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Segments|Keywords
  /// </summary>
  public class PeopleDataAdapter : DataAdapter<PersonM> {
    private readonly PeopleM _model;
    private readonly SegmentsM _segmentsM;
    private readonly KeywordsM _keywordsM;

    public PeopleDataAdapter(SimpleDB.SimpleDB db, PeopleM model, SegmentsM s, KeywordsM k)
      : base("People", db) {
      _model = model;
      _segmentsM = s;
      _keywordsM = k;
    }

    public override void Load() {
      _model.All.Clear();
      LoadFromFile();
    }

    public override void Save() =>
      SaveToFile(_model.GetAll(), ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);
      var person = new PersonM(int.Parse(props[0]), props[1]);
      _model.All.Add(person);
      AllCsv.Add(person, props);
      AllId.Add(person.Id, person);
    }

    private static string ToCsv(PersonM person) =>
      string.Join("|",
        person.Id.ToString(),
        person.Name,
        person.TopSegments == null
          ? string.Empty
          : string.Join(",", person.TopSegments.Cast<SegmentM>().Select(x => x.Id)),
        person.Keywords == null
          ? string.Empty
          : string.Join(",", person.Keywords.Select(x => x.Id)));

    public override void LinkReferences() {
      foreach (var (person, csv) in AllCsv) {
        // Persons top segments
        if (!string.IsNullOrEmpty(csv[2])) {
          var ids = csv[2].Split(',');
          person.TopSegments = new();
          foreach (var segmentId in ids)
            person.TopSegments.Add(_segmentsM.DataAdapter.AllId[int.Parse(segmentId)]);
          person.Segment = (SegmentM)person.TopSegments[0];
        }

        // reference to Keywords
        if (!string.IsNullOrEmpty(csv[3])) {
          var ids = csv[3].Split(',');
          person.Keywords = new(ids.Length);
          foreach (var keywordId in ids)
            person.Keywords.Add(_keywordsM.DataAdapter.AllId[int.Parse(keywordId)]);
        }

        // add loose people
        foreach (var personM in _model.All.Where(x => x.Parent == null)) {
          personM.Parent = _model;
          _model.Items.Add(personM);
        }
      }
    }
  }
}
