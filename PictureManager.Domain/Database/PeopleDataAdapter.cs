﻿using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.TreeCategories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|Name|Segments|Keywords
/// </summary>
public class PeopleDataAdapter : TreeDataAdapter<PersonM> {
  private readonly PeopleTreeCategory _model;

  public event EventHandler<ObjectEventArgs<PersonM[]>> PeopleDeletedEvent = delegate { };

  public PeopleDataAdapter(PeopleTreeCategory model) : base("People", 4) {
    _model = model;
    Core.Db.ReadyEvent += OnDbReady;
  }

  public void RaisePeopleDeleted(ObjectEventArgs<PersonM[]> e) => PeopleDeletedEvent(this, e);

  private void OnDbReady(object sender, EventArgs args) {
    // move all group items to root
    Core.Db.CategoryGroups.ItemDeletedEvent += (_, e) => {
      if (e.Data.Category != Category.People) return;
      foreach (var item in e.Data.Items.ToArray())
        ItemMove(item, _model, false);
    };
  }

  public IEnumerable<PersonM> GetAll() {
    foreach (var cg in _model.Items.OfType<CategoryGroupM>())
      foreach (var personM in cg.Items.Cast<PersonM>())
        yield return personM;

    foreach (var personM in _model.Items.OfType<PersonM>())
      yield return personM;

    foreach (var personM in All.Where(x => x.Id < 0))
      yield return personM;
  }

  public override void Save() =>
    SaveToFile(GetAll());

  public override PersonM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]);

  public override string ToCsv(PersonM person) =>
    string.Join("|",
      person.GetHashCode().ToString(),
      person.Name,
      person.TopSegments == null
        ? string.Empty
        : string.Join(",", person.TopSegments.Select(x => x.GetHashCode().ToString())),
      person.Keywords == null
        ? string.Empty
        : string.Join(",", person.Keywords.Select(x => x.GetHashCode().ToString())));

  public override void LinkReferences() {
    Core.Db.CategoryGroups.LinkGroups(_model, AllDict);

    foreach (var (person, csv) in AllCsv) {
      // Persons top segments
      person.TopSegments = LinkObservableCollection(csv[2], Core.Db.Segments.AllDict);
      if (person.TopSegments != null)
        person.Segment = (SegmentM)person.TopSegments[0];

      // reference to Keywords
      person.Keywords = LinkList(csv[3], Core.Db.Keywords.AllDict);

      // add loose people
      foreach (var personM in AllDict.Values.Where(x => x.Parent == null && x.Id > 0)) {
        personM.Parent = _model;
        _model.Items.Add(personM);
      }
    }
  }

  public override PersonM ItemCreate(ITreeItem parent, string name) =>
    TreeItemCreate(new(GetNextId(), name) { Parent = parent });

  protected override void OnItemDeleted(PersonM item) {
    RaisePeopleDeleted(new(new[] { item })); // TODO why?
    item.Parent.Items.Remove(item);
    item.Parent = null;
    item.Segment = null;
    item.TopSegments = null;
    item.Keywords = null;
  }

  public PersonM GetPerson(string name, bool create) =>
    All.SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
    ?? (create ? ItemCreate(_model, name) : null);
}