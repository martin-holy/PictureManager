﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MH.UI.WPF.BaseClasses;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public class PeopleBaseVM : ObservableObject, ITreeBranch {
    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    private readonly AppCore _coreVM;
    private PersonBaseVM _current;

    public PeopleM Model { get; }
    public Dictionary<int, PersonBaseVM> All { get; } = new();
    public PersonBaseVM Current { get => _current; set { _current = value; OnPropertyChanged(); } }
    public List<PersonBaseVM> Selected { get; } = new();
    public RelayCommand<PersonM> SetCurrentCommand { get; }

    public PeopleBaseVM(AppCore coreVM, PeopleM model) {
      _coreVM = coreVM;
      Model = model;

      Model.Items.CollectionChanged += ModelItems_CollectionChanged;
      Model.PersonDeletedEvent += (_, e) => All.Remove(e.Person.Id);
      SetCurrentCommand = new(person => Current = All[person.Id], person => person != null);

      // load items
      ModelItems_CollectionChanged(Model.Items, null);
    }

    private void ModelItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      // sync Category Groups
      _coreVM.CategoryGroupsBaseVM.SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
      // sync Items
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
    }

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<PersonM, PersonBaseVM>(src, dest, parent,
        (m, vm) => vm.Model.Equals(m),
        m => MH.Utils.Tree.GetDestItem(m, m.Id, All, () => new(m, parent), null));
    }

    public void ToggleKeywordOnSelected(KeywordM keyword) {
      foreach (var person in Selected) {
        Model.ToggleKeyword(person.Model, keyword);
        person.Model.UpdateDisplayKeywords();
      }
    }

    public void Select(List<PersonBaseVM> list, PersonBaseVM p, bool isCtrlOn, bool isShiftOn) =>
      Selecting.Select(Selected, list, p, isCtrlOn, isShiftOn, null);

    public void DeselectAll() => Selecting.DeselectAll(Selected, null);

    public void SetSelected(PersonBaseVM p, bool value) => Selecting.SetSelected(Selected, p, value, null);
  }
}
