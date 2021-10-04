using PictureManager.Domain.Models;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Segments|Keywords
  /// </summary>
  public class PeopleDataAdapter : DataAdapter {
    private readonly Core _core;
    private readonly People _model;

    public PeopleDataAdapter(Core core, People model) : base(nameof(People), core.Sdb) {
      _core = core;
      _model = model;
    }

    public override void Load() {
      _model.All.Clear();
      _model.AllDic = new Dictionary<int, Person>();
      LoadFromFile();
    }

    public override void Save() => SaveToFile(_model.All.Cast<Person>(), ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);
      var person = new Person(int.Parse(props[0]), props[1]) { Csv = props };
      _model.All.Add(person);
      _model.AllDic.Add(person.Id, person);
    }

    public static string ToCsv(Person person) =>
      string.Join("|",
        person.Id.ToString(),
        person.Title,
        person.Segments == null ? string.Empty : string.Join(",", person.Segments.Select(x => x.Id)),
        person.Keywords == null ? string.Empty : string.Join(",", person.Keywords.Select(x => x.Id)));

    public override void LinkReferences() {
      // MediaItems to the Person are added in LinkReferences on MediaItem

      _model.Items.Clear();
      _model.LoadGroupsAndItems(_model.All);

      foreach (var person in _model.All.Cast<Person>()) {
        // Persons top segments
        if (!string.IsNullOrEmpty(person.Csv[2])) {
          var ids = person.Csv[2].Split(',');
          person.Segments = new();
          foreach (var segmentId in ids)
            person.Segments.Add(_core.Segments.AllDic[int.Parse(segmentId)]);
          person.Segment = person.Segments[0];
        }

        // reference to Keywords
        if (!string.IsNullOrEmpty(person.Csv[3])) {
          var ids = person.Csv[3].Split(',');
          person.Keywords = new(ids.Length);
          foreach (var keywordId in ids)
            person.Keywords.Add(_core.Keywords.AllDic[int.Parse(keywordId)]);
        }

        // CSV array is not needed any more
        person.Csv = null;
      }
    }
  }
}
