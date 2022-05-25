using System.Collections.Generic;
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

    public PeopleDataAdapter(PeopleM model, SegmentsM s, KeywordsM k) : base("People", 4) {
      _model = model;
      _segmentsM = s;
      _keywordsM = k;
    }

    public IEnumerable<PersonM> GetAll() {
      foreach (var cg in _model.Items.OfType<CategoryGroupM>())
      foreach (var personM in cg.Items.Cast<PersonM>())
        yield return personM;

      foreach (var personM in _model.Items.OfType<PersonM>())
        yield return personM;
    }

    public override void Save() =>
      SaveToFile(GetAll());

    public override PersonM FromCsv(string[] csv) =>
      new(int.Parse(csv[0]), csv[1]);

    public override string ToCsv(PersonM person) =>
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
      // clear done in CategoryGroups
      //_model.Items.Clear();

      foreach (var (person, csv) in AllCsv) {
        // Persons top segments
        person.TopSegments = LinkObservableCollection(csv[2], _segmentsM.DataAdapter.All);
        if (person.TopSegments != null)
          person.Segment = (SegmentM)person.TopSegments[0];

        // reference to Keywords
        person.Keywords = LinkList(csv[3], _keywordsM.DataAdapter.All);

        // add loose people
        foreach (var personM in All.Values.Where(x => x.Parent == null)) {
          personM.Parent = _model;
          _model.Items.Add(personM);
        }
      }
    }
  }
}
