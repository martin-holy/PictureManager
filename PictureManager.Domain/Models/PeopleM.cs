using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.EventsArgs;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class PeopleM : ObservableObject, ITreeBranch {
    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    public DataAdapter DataAdapter { get; set; }
    public List<PersonM> All { get; } = new();
    public List<PersonM> Selected { get; } = new();
    public Dictionary<int, PersonM> AllDic { get; set; }

    public event EventHandler<PersonDeletedEventArgs> PersonDeletedEvent = delegate { };
    public event EventHandler PeopleKeywordChangedEvent = delegate { };

    private static string GetItemName(object item) => item is PersonM p ? p.Name : string.Empty;

    public IEnumerable<PersonM> GetAll() {
      foreach (var cg in Items.OfType<CategoryGroupM>())
        foreach (var personM in cg.Items.Cast<PersonM>())
          yield return personM;

      foreach (var personM in Items.OfType<PersonM>())
        yield return personM;
    }

    public PersonM ItemCreate(ITreeBranch root, string name) {
      var item = new PersonM(DataAdapter.GetNextId(), name) { Parent = root };
      root.Items.SetInOrder(item, GetItemName);
      All.Add(item);
      DataAdapter.IsModified = true;

      return item;
    }

    public void ItemMove(ITreeLeaf item, ITreeLeaf dest, bool aboveDest) {
      Tree.ItemMove(item, dest, aboveDest, GetItemName);
      DataAdapter.IsModified = true;
    }

    public bool ItemCanRename(string name) =>
      !All.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void ItemRename(PersonM item, string name) {
      item.Name = name;
      item.Parent.Items.SetInOrder(item, GetItemName);
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(PersonM person) {
      person.Parent.Items.Remove(person);
      person.Parent = null;
      person.Segment = null;
      person.Segments = null;
      person.Keywords = null;
      All.Remove(person);
      PersonDeletedEvent(this, new(person));
      DataAdapter.IsModified = true;
    }

    public PersonM GetPerson(string name, bool create) =>
      All.SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
      (create ? ItemCreate(this, name) : null);

    public void DeleteNotUsed(IEnumerable<PersonM> list, IEnumerable<MediaItemM> mediaItems) {
      var people = new HashSet<PersonM>(list);
      foreach (var mi in mediaItems) {
        if (mi.People != null)
          foreach (var personM in mi.People.Where(x => people.Contains(x)))
            people.Remove(personM);

        if (mi.Segments != null)
          foreach (var segment in mi.Segments.Where(x => people.Contains(x.Person)))
            people.Remove(segment.Person);

        if (people.Count == 0) break;
      }

      foreach (var person in people)
        ItemDelete(person);
    }

    public void SegmentPersonChange(SegmentM segment, PersonM oldPerson, PersonM newPerson) {
      if (newPerson != null)
        newPerson.Segment ??= segment;

      if (oldPerson == null) return;

      if (oldPerson.Segment == segment)
        oldPerson.Segment = null;

      if (oldPerson.Segments?.Contains(segment) != true) return;

      oldPerson.Segments = ListExtensions.Toggle(oldPerson.Segments, segment, true);
      oldPerson.OnPropertyChanged(nameof(oldPerson.Segments));
      DataAdapter.IsModified = true;
    }

    public void ToggleSegment(PersonM person, SegmentM segment) {
      person.Segments = ListExtensions.Toggle(person.Segments, segment, true);
      person.OnPropertyChanged(nameof(person.Segments));

      if (person.Segments?.Count > 0)
        person.Segment = person.Segments[0];

      DataAdapter.IsModified = true;
    }

    public void ToggleKeyword(PersonM person, KeywordM keyword) {
      person.Keywords = ListExtensions.Toggle(person.Keywords, keyword, true);
      DataAdapter.IsModified = true;
    }

    public void RemoveKeywordFromPeople(KeywordM keyword) {
      foreach (var person in All.Where(x => x.Keywords?.Contains(keyword) == true))
        ToggleKeyword(person, keyword);

      PeopleKeywordChangedEvent(this, EventArgs.Empty);
    }

    public void ToggleKeywordOnSelected(KeywordM keyword) {
      foreach (var person in Selected) {
        ToggleKeyword(person, keyword);
        person.UpdateDisplayKeywords();
      }

      PeopleKeywordChangedEvent(this, EventArgs.Empty);
    }

    public void Select(List<PersonM> list, PersonM p, bool isCtrlOn, bool isShiftOn) =>
      Selecting.Select(Selected, list, p, isCtrlOn, isShiftOn, null);

    public void DeselectAll() =>
      Selecting.DeselectAll(Selected, null);

    public void SetSelected(PersonM p, bool value) =>
      Selecting.SetSelected(Selected, p, value, null);
  }
}
