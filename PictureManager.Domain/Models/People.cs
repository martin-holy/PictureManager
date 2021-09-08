﻿using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Utils;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class People : BaseCatTreeViewCategory, ITable {
    private List<Person> _selected = new();

    public TableHelper Helper { get; set; }
    public List<IRecord> All { get; } = new();
    public Dictionary<int, Person> AllDic { get; set; }
    public List<Person> Selected => _selected;

    public People() : base(Category.People) {
      Title = "People";
      IconName = IconName.PeopleMultiple;
      CanHaveGroups = true;
      CanCreateItems = true;
      CanRenameItems = true;
      CanDeleteItems = true;
      CanMoveItem = true;
    }

    public void LoadFromFile() {
      All.Clear();
      AllDic = new Dictionary<int, Person>();
      Helper.LoadFromFile();
    }

    public void NewFromCsv(string csv) {
      // ID|Name|Faces|Keywords
      var props = csv.Split('|');
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);
      var person = new Person(int.Parse(props[0]), props[1]) { Csv = props };
      All.Add(person);
      AllDic.Add(person.Id, person);
    }

    public void LinkReferences() {
      // MediaItems to the Person are added in LinkReferences on MediaItem

      Items.Clear();
      LoadGroupsAndItems(All);

      foreach (var person in All.Cast<Person>()) {
        // Persons top faces
        if (!string.IsNullOrEmpty(person.Csv[2])) {
          var ids = person.Csv[2].Split(',');
          person.Faces = new();
          foreach (var faceId in ids)
            person.Faces.Add(Core.Instance.Faces.AllDic[int.Parse(faceId)]);
          person.Face = person.Faces[0];
        }

        // reference to Keywords
        if (!string.IsNullOrEmpty(person.Csv[3])) {
          var ids = person.Csv[3].Split(',');
          person.Keywords = new(ids.Length);
          foreach (var keywordId in ids)
            person.Keywords.Add(Core.Instance.Keywords.AllDic[int.Parse(keywordId)]);
        }

        // CSV array is not needed any more
        person.Csv = null;
      }
    }

    public Person GetPerson(string name, bool create) =>
      Core.Instance.RunOnUiThread(() => {
        var person = All.Cast<Person>().SingleOrDefault(x => x.Title.Equals(name));
        return person ?? (create ? ItemCreate(this, name) as Person : null);
      }).Result;

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      var item = new Person(Helper.GetNextId(), name) { Parent = root };
      var idx = CatTreeViewUtils.SetItemInPlace(root, item);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(All, root, idx);

      All.Insert(allIdx, item);
      Core.Instance.Sdb.SetModified<People>();
      if (root is ICatTreeViewGroup)
        Core.Instance.Sdb.SetModified<CategoryGroups>();

      Core.Instance.Sdb.SaveIdSequences();

      return item;
    }

    public override void ItemDelete(ICatTreeViewItem item) {
      if (item is not Person person) return;

      // remove Person from MediaItems
      if (person.MediaItems.Count > 0) {
        foreach (var mi in person.MediaItems) {
          mi.People.Remove(person);
          if (mi.People.Count == 0)
            mi.People = null;
        }
        Core.Instance.Sdb.SetModified<MediaItems>();
      }

      // remove Person from the tree
      item.Parent.Items.Remove(item);
      if (item.Parent is ICatTreeViewGroup)
        Core.Instance.Sdb.SetModified<CategoryGroups>();
      item.Parent = null;

      // set Person Faces to unknown
      if (person.Faces != null) {
        foreach (var face in person.Faces) {
          face.PersonId = 0;
          face.Person = null;
          Core.Instance.Sdb.SetModified<Faces>();
        }
        person.Faces = null;
      }

      person.Keywords?.Clear();

      // remove Person from DB
      All.Remove(person);

      Core.Instance.Sdb.SetModified<People>();
    }

    public void Select(List<Person> list, Person p, bool isCtrlOn, bool isShiftOn) =>
      Selecting.Select<Person>(ref _selected, list, p, isCtrlOn, isShiftOn, null);

    public void DeselectAll() => Selecting.DeselectAll<Person>(ref _selected, null);

    public void SetSelected(Person p, bool value) => Selecting.SetSelected<Person>(ref _selected, p, value, null);

    /// <summary>
    /// Toggle Person on Media Item
    /// </summary>
    /// <param name="p">Person</param>
    /// <param name="mi">Media Item</param>
    public static void Toggle(Person p, MediaItem mi) {
      if (p.IsMarked) {
        mi.People ??= new();
        mi.People.Add(p);
        p.MediaItems.Add(mi);
      }
      else {
        mi.People?.Remove(p);
        p.MediaItems.Remove(mi);
        if (mi.People?.Count == 0)
          mi.People = null;
      }
    }

    public void ToggleKeywordOnSelected(Keyword keyword) {
      foreach (var person in Selected) {
        ToggleKeyword(person, keyword);
        person.UpdateDisplayKeywords();
      }
    }

    public static void ToggleKeyword(Person person, Keyword keyword) {
      var currentKeywords = person.Keywords;
      Keywords.Toggle(keyword, ref currentKeywords, null, null);
      person.Keywords = currentKeywords;
      Core.Instance.Sdb.SetModified<People>();
    }
  }
}
