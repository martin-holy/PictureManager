using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public class PersonBaseVM : ObservableObject, ISelectable, ITreeLeaf {
    #region ISelectable implementation
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    #endregion

    #region ITreeLeaf implementation
    public ITreeBranch Parent { get; set; }
    #endregion

    public PersonM Model { get; }
    public ObservableCollection<KeywordM> DisplayKeywords { get; set; }
    
    public PersonBaseVM(PersonM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }

    public void UpdateDisplayKeywords() {
      DisplayKeywords?.Clear();

      if (Model.Keywords == null) {
        DisplayKeywords = null;
        OnPropertyChanged(nameof(DisplayKeywords));
        return;
      }

      DisplayKeywords ??= new();
      OnPropertyChanged(nameof(DisplayKeywords));
      var allKeywords = new List<KeywordM>();

      foreach (var keyword in Model.Keywords)
        MH.Utils.Tree.GetThisAndItemsRecursive(keyword, ref allKeywords);

      foreach (var keyword in allKeywords.Distinct().OrderBy(x => x.FullName))
        DisplayKeywords.Add(keyword);
    }
  }
}
